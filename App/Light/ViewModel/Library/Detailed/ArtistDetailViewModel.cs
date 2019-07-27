using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.Model;
using Light.Shell;
using Microsoft.EntityFrameworkCore;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Light.View.Library.Detailed;
using Microsoft.Extensions.DependencyInjection;

namespace Light.ViewModel.Library.Detailed
{
    /// <summary>
    /// Artist detailed information view model.
    /// </summary>
    public class ArtistDetailViewModel : DetailedViewModelBase
    {
        #region Private Definitions
        private string _artistName;
        private string _artistBio;
        private string _totalAlbums;
        private string _totalDuration;
        private readonly int _itemId;
        private DbArtist _backendArtist;
        private ThumbnailTag _backgroundImageTag;
        #endregion

        #region Artist Detail View Fields
        public string ArtistName
        {
            get
            {
                return _artistName;
            }
            set
            {
                if (_artistName == value) return;
                _artistName = value;
                NotifyChange(nameof(ArtistName));
            }
        }
        public string ArtistBio
        {
            get
            {
                return _artistBio;
            }
            set
            {
                if (_artistBio == value) return;
                _artistBio = value;
                NotifyChange(nameof(ArtistBio));
            }
        }

        public string TotalAlbums
        {
            get { return _totalAlbums; }
            set
            {
                if (_totalAlbums == value) return;
                _totalAlbums = value;
                NotifyChange(nameof(TotalAlbums));
            }
        }

        public string TotalDuration
        {
            get { return _totalDuration; }
            set
            {
                if (_totalDuration == value) return;
                _totalDuration = value;
                NotifyChange(nameof(TotalDuration));
            }
        }

        public ThumbnailTag BackgroundImageTag
        {
            get { return _backgroundImageTag; }
            set
            {
                _backgroundImageTag = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Binding Collections
        #region Private Definitions

        private ObservableCollection<DbAlbum> _artistAlbums;
        //private ObservableCollection<CommonViewItemModel> _artistSongs;

        #endregion

        #region Artist Property View Fields

        public ObservableCollection<DbAlbum> ArtistAlbums
        {
            get { return _artistAlbums; }
            set
            {
                if (_artistAlbums == value) return;
                _artistAlbums = value;
                NotifyChange(nameof(ArtistAlbums));
            }
        }
        //public ObservableCollection<CommonViewItemModel> ArtistSongs
        //{
        //    get { return _artistSongs; }
        //    set
        //    {
        //        if (_artistSongs == value) return;
        //        _artistSongs = value;
        //        NotifyChange(nameof(ArtistSongs));
        //    }
        //}
        #endregion

        #endregion

        /// <summary>
        /// Current album ID in database.
        /// </summary>
        public int ItemId => _itemId;

        /// <summary>
        /// Command for Album item clicks.
        /// </summary>
        public ICommand AlbumItemCommand;

        int ParseWithFallback(string s)
        {
            int result = 0;
            if (int.TryParse(s, out result))
            {
                return result;
            }
            return 0;
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="itemId">Album item ID.</param>
        /// <param name="frame">Navigation frame.</param>
        public ArtistDetailViewModel(int itemId, Frame frame) : base(Window.Current.Dispatcher)
        {
            IsLoading = true;

            _itemId = itemId;
            ArtistName = ArtistBio = string.Empty;

            ArtistAlbums = new ObservableCollection<DbAlbum>();
            //ArtistSongs = new ObservableCollection<CommonViewItemModel>();
            AlbumItemCommand = new AlbumItemClickedCommand(frame);

            RegisterMessages();
        }

        public async Task LoadTitleAsync(bool artistFallbackLarge = false)
        {
            _backendArtist = await _itemId.GetArtistByIdAsync();
            if (_backendArtist == null)
            {
                return;
            }

            ArtistBio = !string.IsNullOrEmpty(_backendArtist.Intro) ? _backendArtist.Intro : string.Empty;
            ArtistName = _backendArtist.Name;
            BackgroundImageTag = new ThumbnailTag
            {
                Fallback = artistFallbackLarge ? "Artist,DefaultArtistLarge" : "Artist,ArtistPlaceholder",
                ArtistName = _backendArtist.Name,
            };
        }

        public async Task LoadContentAsync()
        {
            if (_backendArtist == null)
            {
                return;
            }

            HashSet<string> knownAlbums = new HashSet<string>();

            var albumAsyncEnumerator = _backendArtist.Albums.ToAsyncEnumerable().GetEnumerator();
            if (albumAsyncEnumerator != null)
            {
                while (await albumAsyncEnumerator.MoveNext())
                {
                    var album = albumAsyncEnumerator.Current;
                    album.MediaFiles = album.MediaFiles
                        .OrderBy(x => ParseWithFallback(x.DiscNumber))
                        .ThenBy(x => ParseWithFallback(x.TrackNumber))
                        .ToList();
                    knownAlbums.Add(album.Title);
                    ArtistAlbums.Add(album);
                }
            }

            List<DbMediaFile> otherMusic = new List<DbMediaFile>();

            TimeSpan totalTime = TimeSpan.Zero;

            var artistAsyncEnumerator = _backendArtist.MediaFiles.ToAsyncEnumerable().GetEnumerator();
            if (artistAsyncEnumerator != null)
            {
                while (await artistAsyncEnumerator.MoveNext())
                {
                    var current = artistAsyncEnumerator.Current;
                    if (!knownAlbums.Contains(current.Album))
                    {
                        otherMusic.Add(current);
                    }
                    totalTime += current.Duration;
                }
            }

            var otherAlbums = otherMusic
                .GroupBy(x => x.Album)
                .Select(x =>
                {
                    var f = x.First();
                    return new DbAlbum()
                    {
                        Title = f.Album,
                        Artist = f.AlbumArtist,
                        Date = f.Date,
                        MediaFiles = x
                        .OrderBy(k => ParseWithFallback(k.DiscNumber))
                        .ThenBy(k => ParseWithFallback(k.TrackNumber))
                        .ToList()
                    };
                });

            foreach (var other in otherAlbums)
            {
                ArtistAlbums.Add(other);
            }

            TotalAlbums = ArtistAlbums.Count.ToString();

            TotalDuration = $"{(int)totalTime.TotalMinutes}:{totalTime.Seconds:00}";
            IsLoading = false;
            DesktopTitleViewConfiguration.SetTitleBarText(ArtistName);
        }

        /// <summary>
        /// Method for loading current album data.
        /// </summary>
        /// <returns></returns>
        public async Task LoadDataAsync(bool artistFallbackLarge = true)
        {
            await LoadTitleAsync(artistFallbackLarge);
            await LoadContentAsync();
        }

        #region Share
        public void RegisterMessages()
        {
            Messenger.Default.Register<MessageBase>(this,
                CommonSharedStrings.ControlPageShareClickedEventMessageToken, OnControlPageShareClickedReceived);
        }

        /// <summary>
        /// Event handler for artist sharing.
        /// </summary>
        /// <param name="obj"></param>
        private void OnControlPageShareClickedReceived(MessageBase obj)
        {
            _backendArtist.Share();
        }
        #endregion

        #region Online data
        /// <summary>
        /// Load artist online data.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        private async Task LoadArtistOnlineContentAsync(DbArtist elem)
        {
            // Check internet connection
            if (InternetConnectivityDetector.HasInternetConnection)
            {
                // Load Online content
                try
                {
                    var modifyFlag = false;

                    if (string.IsNullOrEmpty(elem.Intro))
                    {
                        elem = await elem.LoadOnlineArtistInfoAsync();
                        modifyFlag = !string.IsNullOrEmpty(elem.Intro);
                    }

                    if (!modifyFlag) return;

                    ArtistBio = elem.Intro;

                    await CommitOnlineData(elem);
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }
        }

        /// <summary>
        /// Write online data into database.
        /// </summary>
        /// <param name="elem"></param>
        /// <returns></returns>
        private async Task CommitOnlineData(DbArtist elem)
        {
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                try
                {
                    var query = context.Artists.Where(i => i.Id == elem.Id);
                    if (query.Any())
                    {
                        var item = query.FirstOrDefault();
                        context.Entry(item).Entity.Intro = elem.Intro;
                        // ArtistImagePath is the absoulte path on disk.
                        context.Entry(item).Entity.ImagePath = elem.ImagePath;
                        context.Entry(item).State = EntityState.Modified;
                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }
        }
        #endregion

        /// <summary>
        /// Method to perform cleanup operations.
        /// </summary>
        public override void Cleanup()
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.ControlPageShareClickedEventMessageToken, OnControlPageShareClickedReceived);

            base.Cleanup();
        }

        /// <summary>
        /// Command for album item clicks.
        /// </summary>
        public class AlbumItemClickedCommand : ICommand
        {
            private readonly Frame _parentFrame;

            /// <summary>
            /// Class constructor.
            /// </summary>
            /// <param name="frame">Navigation frame.</param>
            public AlbumItemClickedCommand(Frame frame)
            {
                _parentFrame = frame;
            }

            /// <summary>
            /// Determine whether the command can be executed.
            /// </summary>
            /// <param name="parameter">Current command parameter.</param>
            /// <returns>An value indicates whether the command can be executed with this parameter.</returns>
            public bool CanExecute(object parameter)
            {
                return parameter is int || parameter is ItemClickEventArgs;
            }

            /// <summary>
            /// Event handler for clicks.
            /// </summary>
            /// <param name="parameter"></param>
            public void Execute(object parameter)
            {
                if (parameter is int)
                {
                    _parentFrame.Navigate(typeof(AlbumDetailView), (int)parameter, new DrillInNavigationTransitionInfo());
                }
                else if (parameter is ItemClickEventArgs)
                {
                    _parentFrame.Navigate(typeof(AlbumDetailView), ((CommonViewItemModel)((ItemClickEventArgs)parameter).ClickedItem).InternalDbEntityId, new DrillInNavigationTransitionInfo());
                }
            }

#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        }
    }
}
