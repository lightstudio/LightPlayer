using Light.Common;
using Light.Managed.Database.Native;
using Light.Managed.Library;
using Light.Managed.Online;
using Light.Managed.Tools;
using Light.Utilities;
using Light.Utilities.UserInterfaceExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Media.Imaging;

namespace Light.Core
{
    public class ThumbnailOperations : IThumbnailOperations
    {
        public Task FetchAlbumAsync(string artist, string album, string filePath)
        {
            return ThumbnailAgent.FetchAsync(artist, album, filePath, false);
        }

        public Task RemoveAlbumAsync(string artist, string album)
        {
            return ThumbnailManager.RemoveAsync(artist, album, false);
        }

        public Task RemoveArtistAsync(string artist)
        {
            return ThumbnailManager.RemoveAsync(artist, false);
        }
    }

    /// <summary>
    /// Singleton class to handle download tasks.
    /// </summary>
    public class ThumbnailAgent
    {
        private static Dictionary<string, Task> _downloadTasks = new Dictionary<string, Task>();

        private static async Task InternalFetchAlbumAsync(string artist, string album, string filePath, bool online)
        {
            if (filePath != null)
            {
                var file = await NativeMethods.GetStorageFileFromPathAsync(filePath);

                if (file != null)
                {
                    using (var coverStream = await CoverExtractor.GetCoverStreamFromFileAsync(file))
                    {
                        if (coverStream != null)
                        {
                            using (var sr = new BinaryReader(coverStream.AsStream()))
                            {
                                var content = sr.ReadBytes((int)coverStream.Size);
                                await ThumbnailManager.AddAsync(artist, album, content, true);
                                return;
                            }
                        }
                    }

                    // The file itself does not contain a cover image.
                    // Search in parent folder instead.
                    if (file is IStorageItem2)
                    {
                        var parent = await (file as IStorageItem2).GetParentAsync();
                        if (parent != null)
                        {
                            var f =
                                await parent.TryGetItemAsync(CommonSharedStrings.FolderJpg) as IStorageFile ??
                                await parent.TryGetItemAsync(CommonSharedStrings.CoverJpg) as IStorageFile ??
                                await parent.TryGetItemAsync(CommonSharedStrings.FolderPng) as IStorageFile ??
                                await parent.TryGetItemAsync(CommonSharedStrings.CoverPng) as IStorageFile;
                            if (f != null)
                            {
                                using (var s = await f.OpenReadAsync())
                                using (var sr = new BinaryReader(s.AsStream()))
                                {
                                    var content = sr.ReadBytes((int)s.Size);
                                    await ThumbnailManager.AddAsync(artist, album, content, true);
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            if (online && AggreatedOnlineMetadata.Availability)
            {
                // There is no cover image in the parent directory, or the parent directory cannot be retrieved.
                // Search online
                try
                {
                    var searchResult = (await AggreatedOnlineMetadata.GetAlbumsAsync(album, artist)).FirstOrDefault();

                    if (searchResult != null)
                    {
                        using (var client = new HttpClient())
                        {
                            using (var response = await client.GetAsync(searchResult.Thumbnail))
                            {
                                response.EnsureSuccessStatusCode();
                                response.Content.Headers.TryGetValues("Content-Length", out var ctlength);
                                var lengthValue = ctlength.FirstOrDefault();

                                if (lengthValue == null ||
                                    !ulong.TryParse(lengthValue, out ulong length))
                                {
                                    length = 0;
                                }

                                if (length <= 10 * 1024 * 1024)
                                {
                                    var content = await response.Content.ReadAsByteArrayAsync();

                                    if (content.Length >= (int)length)
                                    {
                                        await ThumbnailManager.AddAsync(artist, album, content, true);
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
                catch { }
                // Only write placeholder file when we don't have online results.
                await ThumbnailManager.AddAsync(artist, album, new byte[0], true);
            }
        }

        private static async Task InternalFetchArtistAsync(string artist)
        {
            try
            {
                var searchResult = (await AggreatedOnlineMetadata.GetArtistsAsync(artist)).FirstOrDefault();

                if (searchResult != null)
                {
                    using (var client = new HttpClient())
                    {
                        using (var response = await client.GetAsync(searchResult.Thumbnail))
                        {
                            response.EnsureSuccessStatusCode();
                            response.Content.Headers.TryGetValues("Content-Length", out var ctlength);
                            var lengthValue = ctlength.FirstOrDefault();

                            if (lengthValue == null ||
                                !ulong.TryParse(lengthValue, out ulong length))
                            {
                                length = 0;
                            }

                            if (length <= 10 * 1024 * 1024)
                            {
                                var content = await response.Content.ReadAsByteArrayAsync();

                                if (content.Length >= (int)length)
                                {
                                    await ThumbnailManager.AddAsync(artist, content, true);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            await ThumbnailManager.AddAsync(artist, new byte[0], true);
        }

        public static async void Fetch(string artist, string album, string filePath, bool online = true)
        {
            var tname = ThumbnailManager.FormatAlbumCacheName(artist, album);
            Task task;
            lock (_downloadTasks)
            {
                if (_downloadTasks.ContainsKey(tname))
                {
                    return;
                }
                else
                {
                    task = InternalFetchAlbumAsync(artist, album, filePath, online);
                    _downloadTasks.Add(tname, task);
                }
            }
            await task;
            lock (_downloadTasks)
            {
                _downloadTasks.Remove(tname);
            }
        }

        public static async Task FetchAsync(string artist, string album, string filePath, bool online = true)
        {
            var tname = ThumbnailManager.FormatAlbumCacheName(artist, album);
            Task task;
            bool remove;
            lock (_downloadTasks)
            {
                if (remove = !_downloadTasks.TryGetValue(tname, out task))
                {
                    task = InternalFetchAlbumAsync(artist, album, filePath, online);
                    _downloadTasks.Add(tname, task);
                }
            }
            await task;
            if (remove)
            {
                lock (_downloadTasks)
                {
                    _downloadTasks.Remove(tname);
                }
            }
        }

        public static async void Fetch(string artist)
        {
            var tname = ThumbnailManager.FormatArtistCacheName(artist);
            Task task;
            lock (_downloadTasks)
            {
                if (_downloadTasks.ContainsKey(tname))
                {
                    return;
                }
                else
                {
                    task = InternalFetchArtistAsync(artist);
                    _downloadTasks.Add(tname, task);
                }
            }
            await task;
            lock (_downloadTasks)
            {
                _downloadTasks.Remove(tname);
            }
        }
    }
}
