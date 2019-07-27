using Light.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;

namespace Light.Core
{
    public class ThumbnailManager
    {
        const string AlbumThumbnailNameFormat = "{0}.{1}.Album.jpg";
        const string ArtistThumbnailNameFormat = "{0}.Artist.jpg";
        static AsyncLock _sync = new AsyncLock();
        static ConcurrentLRUCache<string, BitmapImage> _bitmapDiskCache =
            new ConcurrentLRUCache<string, BitmapImage>(50);

        public delegate void AlbumImageChangedEventHandler(string artistName, string albumName, bool hasImage);
        public delegate void ArtistImageChangedEventHandler(string artistName, bool hasImage);

        private static Dictionary<string, AlbumImageChangedEventHandler> AlbumImageChanged = new Dictionary<string, AlbumImageChangedEventHandler>();
        private static Dictionary<string, ArtistImageChangedEventHandler> ArtistImageChanged = new Dictionary<string, ArtistImageChangedEventHandler>();

        private static string FileNameEscape(string fileName)
        {
            return Path.GetInvalidFileNameChars().Aggregate(fileName, (current, c) => current.Replace(c, '-'));
        }

        private static async Task<StorageFolder> EnsureThumbnailFolderCreatedAsync()
        {
            var item = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync("Thumbnail");
            if (item == null)
            {
                return await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("Thumbnail");
            }
            else
            {
                if (item is StorageFolder)
                {
                    return (StorageFolder)item;
                }
                else
                {
                    await item.DeleteAsync();
                    return await ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("Thumbnail");
                }
            }
        }

        private static async Task<BitmapImage> TryDecodeAsync(IRandomAccessStream stream, int thumbnailDecodeSize)
        {
            try
            {
                var image = new BitmapImage()
                {
                    DecodePixelType = DecodePixelType.Logical
                };
                if (thumbnailDecodeSize > 0)
                {
                    image.DecodePixelWidth = thumbnailDecodeSize;
                }
                await image.SetSourceAsync(stream);
                return image;
            }
            catch
            {
                return null;
            }
        }

        private static async Task InternalAddAsync(string cacheName, byte[] file, bool overwrite)
        {
            using (await _sync.LockAsync())
            {
                var folder = await EnsureThumbnailFolderCreatedAsync();
                var f = await folder.TryGetItemAsync(cacheName);

                if (f != null)
                {
                    if (overwrite)
                    {
                        await f.DeleteAsync();
                    }
                    else
                    {
                        return;
                    }
                }

                var newFile = await folder.CreateFileAsync(cacheName);
                if (file.Length > 0)
                {
                    using (var stream = await newFile.OpenStreamForWriteAsync())
                    {
                        await stream.WriteAsync(file, 0, file.Length);
                    }
                }

                _bitmapDiskCache.TryRemove(cacheName);
            }
        }

        public static string FormatAlbumCacheName(string artistName, string albumName)
        {
            return string.Format(
                AlbumThumbnailNameFormat,
                FileNameEscape(artistName),
                FileNameEscape(albumName));
        }

        public static string FormatArtistCacheName(string artistName)
        {
            return string.Format(
                ArtistThumbnailNameFormat,
                FileNameEscape(artistName));
        }

        public static async Task AddAsync(string artistName, string albumName, byte[] file, bool overwrite = true)
        {
            var cacheName = FormatAlbumCacheName(artistName, albumName);
            await InternalAddAsync(cacheName, file, overwrite);
            AlbumImageChangedEventHandler handler;
            lock (AlbumImageChanged)
            {
                AlbumImageChanged.TryGetValue(cacheName, out handler);
            }
            handler?.Invoke(artistName, albumName, file != null && file.Length > 0);
        }

        public static async Task AddAsync(string artistName, byte[] file, bool overwrite = true)
        {
            var cacheName = FormatArtistCacheName(artistName);
            await InternalAddAsync(cacheName, file, overwrite);
            ArtistImageChangedEventHandler handler;
            lock (ArtistImageChanged)
            {
                ArtistImageChanged.TryGetValue(cacheName, out handler);
            }
            handler?.Invoke(artistName, file != null && file.Length > 0);
        }

        public static async Task<(BitmapImage, bool)> InternalGetAsync(string cacheName, int requiredSize)
        {
            using (await _sync.LockAsync())
            {
                if (_bitmapDiskCache.TryGetValue(cacheName, out var cachedValue))
                {
                    if (cachedValue == null || cachedValue.DecodePixelWidth == 0 || cachedValue.DecodePixelWidth >= requiredSize)
                    {
                        return (cachedValue, true);
                    }
                    else
                    {
                        _bitmapDiskCache.TryRemove(cacheName);
                    }
                }
                var folder = await EnsureThumbnailFolderCreatedAsync();
                var item = await folder.TryGetItemAsync(cacheName);
                if (item == null || !(item is StorageFile))
                {
                    return (null, false);
                }

                var file = (StorageFile)item;

                using (var stream = await file.OpenReadAsync())
                {
                    if (stream.Size == 0)
                    {
                        _bitmapDiskCache.Add(cacheName, null);
                        return (null, true);
                    }

                    var img = await TryDecodeAsync(stream, requiredSize);
                    _bitmapDiskCache.Add(cacheName, img);
                    return (img, true);
                }
            }
        }

        public static Task<(BitmapImage, bool)> GetAsync(string artistName, string albumName, int requiredSize)
        {
            var cacheName = FormatAlbumCacheName(artistName, albumName);
            return InternalGetAsync(cacheName, requiredSize);
        }

        public static Task<(BitmapImage, bool)> GetAsync(string artistName, int requiredSize)
        {
            var cacheName = FormatArtistCacheName(artistName);
            return InternalGetAsync(cacheName, requiredSize);
        }

        public static async Task InternalRemoveAsync(string cacheName, bool emptyFile)
        {
            using (await _sync.LockAsync())
            {
                _bitmapDiskCache.TryRemove(cacheName);
                var folder = await EnsureThumbnailFolderCreatedAsync();
                var file = await folder.TryGetItemAsync(cacheName);
                if (file != null)
                {
                    await file.DeleteAsync();
                }

                if (emptyFile)
                {
                    await folder.CreateFileAsync(cacheName);
                }
            }
        }

        public static async Task RemoveAsync(string artistName, string albumName, bool emptyFile)
        {
            var cacheName = FormatAlbumCacheName(artistName, albumName);
            await InternalRemoveAsync(cacheName, emptyFile);
            AlbumImageChangedEventHandler handler;
            lock (AlbumImageChanged)
            {
                AlbumImageChanged.TryGetValue(cacheName, out handler);
            }
            handler?.Invoke(artistName, albumName, false);
        }

        public static async Task RemoveAsync(string artistName, bool emptyFile)
        {
            var cacheName = FormatArtistCacheName(artistName);
            await InternalRemoveAsync(cacheName, emptyFile);
            ArtistImageChangedEventHandler handler;
            lock (ArtistImageChanged)
            {
                ArtistImageChanged.TryGetValue(cacheName, out handler);
            }
            handler?.Invoke(artistName, false);
        }

        public static void OnAlbumImageChanged(string artistName, string albumName, AlbumImageChangedEventHandler callback)
        {
            var cacheName = FormatAlbumCacheName(artistName, albumName);
            lock (AlbumImageChanged)
            {
                if (AlbumImageChanged.ContainsKey(cacheName))
                {
                    AlbumImageChanged[cacheName] += callback;
                }
                else
                {
                    AlbumImageChanged.Add(cacheName, callback);
                }
            }
        }

        public static void OnArtistImageChanged(string artistName, ArtistImageChangedEventHandler callback)
        {
            var cacheName = FormatArtistCacheName(artistName);
            lock (ArtistImageChanged)
            {
                if (ArtistImageChanged.ContainsKey(cacheName))
                {
                    ArtistImageChanged[cacheName] += callback;
                }
                else
                {
                    ArtistImageChanged.Add(cacheName, callback);
                }
            }
        }

        public static void RemoveHandler(string artistName, string albumName, AlbumImageChangedEventHandler callback)
        {
            var cacheName = FormatAlbumCacheName(artistName, albumName);
            lock (AlbumImageChanged)
            {
                if (AlbumImageChanged.TryGetValue(cacheName, out var d))
                {
                    d -= callback;
                    if (d == null)
                    {
                        AlbumImageChanged.Remove(cacheName);
                    }
                    else
                    {
                        AlbumImageChanged[cacheName] = d;
                    }
                }
            }
        }

        public static void RemoveHandler(string artistName, ArtistImageChangedEventHandler callback)
        {
            var cacheName = FormatArtistCacheName(artistName);
            lock (ArtistImageChanged)
            {
                if (ArtistImageChanged.TryGetValue(cacheName, out var d))
                {
                    d -= callback;
                    if (d == null)
                    {
                        ArtistImageChanged.Remove(cacheName);
                    }
                    else
                    {
                        ArtistImageChanged[cacheName] = d;
                    }
                }
            }
        }
    }
}
