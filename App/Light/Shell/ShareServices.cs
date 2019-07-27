using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Light.Managed.Database.Entities;
using Light.Common;
using Light.Managed.Tools;
using Light.Model;
using Light.Converter;
using System.Linq;

namespace Light.Shell
{
    /// <summary>
    /// Common Share Services.
    /// </summary>
    public sealed class ShareServices
    {
        private const string DesktopShareFailureTextId = "DesktopShareFailureText";
        private static Dictionary<int, ShareServices> _viewDict = new Dictionary<int, ShareServices>();

        /// <summary>
        /// Get ShareServices instance for current view.
        /// </summary>
        /// <returns></returns>
        public static ShareServices GetForCurrentView()
        {
            var viewId = ApplicationView.GetForCurrentView().Id;
            if (_viewDict.ContainsKey(viewId))
            {
                return _viewDict[viewId];
            }
            else
            {
                var instance = new ShareServices();
                _viewDict.Add(viewId, instance);
                return instance;
            }
        }

        private readonly List<StorageFile> _files;
        private readonly DataTransferManager _dtm;

        /// <summary>
        /// Content title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Content description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ShareServices()
        {
            _dtm = DataTransferManager.GetForCurrentView();

            _files = new List<StorageFile>();

            Title = string.Empty;
            Description = string.Empty;

            _dtm.DataRequested += DtmOnDataRequested;
        }

        /// <summary>
        /// Add single file to share file lists.
        /// </summary>
        /// <param name="path">The file's path.</param>
        /// <returns>An awaitable task. Upon finishing, a boolean result indicating action result will be returned.</returns>
        public async Task<bool> AddFileAsync(string path)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                _files.Add(file);

                return true;
            }
            catch (COMException)
            {
                // WinRT exception: ignore
            }
            catch (FileNotFoundException)
            {
                // File not found: ignore
            }
            catch (UnauthorizedAccessException)
            {
                // Unable to access: ignore
            }

            return false;
        }

        /// <summary>
        /// Add single file to share file lists.
        /// </summary>
        /// <param name="file">The file to be added.</param>
        public void AddFile(StorageFile file)
        {
            _files.Add(file);
        }

        /// <summary>
        /// Add batch of files.
        /// </summary>
        /// <param name="files">Files to be added.</param>
        public void AddFiles(IEnumerable<StorageFile> files)
        {
            _files.AddRange(files);
        }

        /// <summary>
        /// Set content for artist.
        /// </summary>
        /// <param name="item">The artist information to be shared.</param>
        public void SetArtistContent(DbArtist artist)
        {
            Title = artist.Name;
            Description = artist.Intro ?? string.Empty;
        }

        /// <summary>
        /// Show share UI if it is supported.
        /// </summary>
        public void ShowShareUI()
        {
            if (PlatformInfo.IsRedstoneRelease)
            {
                if (!DataTransferManager.IsSupported())
                {
                    return;
                }
            }

            DataTransferManager.ShowShareUI();
        }

        /// <summary>
        /// Method for pre-cleaning.
        /// </summary>
        public void PrecleanForSession()
        {
            _files.Clear();
            Title = string.Empty;
            Description = string.Empty;
        }

        /// <summary>
        /// Event handler for content requested.
        /// </summary>
        /// <param name="sender">Instance of <see cref="DataTransferManager"/>.</param>
        /// <param name="args">Instance of <see cref="DataRequestedEventArgs"/>.</param>
        private void DtmOnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            var deferral = args.Request.GetDeferral();

            try
            {
                if (!string.IsNullOrEmpty(Title))
                {
                    // Send all files.
                    if (_files.Count > 0)
                    {
                        // Fix for .NET Native: Force cast
                        args.Request.Data.SetStorageItems(_files.Select(i => i as IStorageItem));
                    }

                    args.Request.Data.Properties.Title = Title;

                    if (Description != string.Empty)
                    {
                        args.Request.Data.Properties.Description = Description;
                        args.Request.Data.SetText(Description);
                    }
                    else
                    {
                        args.Request.Data.Properties.Description = Title;
                        args.Request.Data.SetText(Title);
                    }

                    deferral.Complete();
                    return;
                }
            }
            catch (COMException ex)
            {
                // TODO: Log error
                TelemetryHelper.TrackExceptionAsync(ex);
            }

            // Share contract is only supported on specific platforms
            if (PlatformInfo.CurrentPlatform == Platform.WindowsDesktop)
            {
#pragma warning disable CS0618 // It works on desktop devices
                args.Request.FailWithDisplayText(DesktopShareFailureTextId);
#pragma warning restore CS0618
            }
        }
    }

    /// <summary>
    /// Entity extensions.
    /// </summary>
    public static class ShareEntityExtensions
    {
        private const string SongDescriptionFieldId = "SongSubtitleFormat";

        /// <summary>
        /// Share an album.
        /// </summary>
        /// <param name="album">The album to be shared.</param>
        public static async Task ShareAsync(this DbAlbum album)
        {
            if (album != null)
            {
                var files = new List<StorageFile>();
                // Prep files.
                foreach (var et in album.MediaFiles)
                {
                    try
                    {
                        files.Add(await StorageFile.GetFileFromPathAsync(et.Path));
                    }
                    catch (COMException)
                    {
                        // WinRT error: ignore
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // WinRT error: ignore
                    }
                    catch (FileNotFoundException)
                    {
                        // File not found: ignore
                    }
                }

                var shareService = ShareServices.GetForCurrentView();
                shareService.PrecleanForSession();
                shareService.Title = album.Title;
                shareService.Description = album.Intro ?? string.Empty;
                shareService.AddFiles(files);
                shareService.ShowShareUI();
            }
        }

        /// <summary>
        /// Share an artist.
        /// </summary>
        /// <param name="artist">The artist to be shared.</param>
        public static void Share(this DbArtist artist)
        {
            if (artist != null)
            {
                var shareService = ShareServices.GetForCurrentView();
                shareService.PrecleanForSession();
                shareService.SetArtistContent(artist);
                shareService.ShowShareUI();
            }
        }

        /// <summary>
        /// Share a file.
        /// </summary>
        /// <param name="file">The file to be shared.</param>
        /// <returns>An awaitable task.</returns>
        public static async Task ShareAsync(this DbMediaFile fileEntity)
        {
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            // FIXME: External file should still set this field, but they should retrieve file from MRU.
            // Currently no external files will be shared.
            await shareService.AddFileAsync(fileEntity.Path);
            shareService.Title = fileEntity.Title;
            shareService.Description = string.Format(CommonSharedStrings.GetString(SongDescriptionFieldId),
                fileEntity.Album,
                fileEntity.AlbumArtist,
                MiliSecToNormalTimeConverter.GetTimeStringFromTimeSpanOrDouble(fileEntity.Duration));
            shareService.ShowShareUI();
        }

        /// <summary>
        /// Share a item.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static async Task ShareAsync(this CommonViewItemModel model)
        {
            switch (model?.Type)
            {
                case CommonItemType.Album:
                    var album = await model.InternalDbEntityId.GetAlbumByIdAsync();
                    await album.ShareAsync();
                    break;
                case CommonItemType.Artist:
                    var artist = await model.InternalDbEntityId.GetArtistByIdAsync();
                    artist.Share();
                    break;
                case CommonItemType.Song:
                    var fileEntity = await model.InternalDbEntityId.GetFileByIdAsync();
                    await fileEntity.ShareAsync();
                    break;
            }
        }
    }
}
