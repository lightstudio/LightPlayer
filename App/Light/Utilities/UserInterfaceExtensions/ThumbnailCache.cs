using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Light.Common;
using Light.Managed.Tools;
using Windows.Graphics.Display;
using Light.Managed.Database.Native;
using System.Net.Http;
using System.Linq;

namespace Light.Utilities.UserInterfaceExtensions
{
    static class ThumbnailCache
    {
        static ConcurrentLRUCache<string, BitmapImage> _bitmapDiskCache =
            new ConcurrentLRUCache<string, BitmapImage>(100);
        static Tuple<BitmapImage, bool> CacheDisabled = new Tuple<BitmapImage, bool>(null, true);
        static Tuple<BitmapImage, bool> CacheMiss = new Tuple<BitmapImage, bool>(null, false);

        static StorageFolder localCache = ApplicationData.Current.LocalCacheFolder;

        const int MaxOnlineImageSize = 1048576;

        static ThumbnailCache()
        {
            //DisplayInformation.GetForCurrentView().DpiChanged += OnDpiChanged;
        }

        private static void OnDpiChanged(DisplayInformation sender, object args)
        {
            _bitmapDiskCache.Clear();
        }

        static private async Task<BitmapImage> CreateBitmapFromStreamAsync(IRandomAccessStream stream)
        {
            try
            {
                var img = new BitmapImage();
                img.DecodePixelType = DecodePixelType.Logical;
                img.DecodePixelWidth = 540;
                await img.SetSourceAsync(stream);
                return img;
            }
            catch
            {
                return null;
            }
        }

        static public async Task<IRandomAccessStream> RetrieveStorageFileAsStreamAsync(StorageFile file, bool parentFolder)
        {
            var coverStream = await CoverExtractor.GetCoverStreamFromFileAsync(file);
            if (coverStream != null)
            {
                return coverStream;
            }

            if (parentFolder)
            {
                var parent = await file.GetParentAsync();
                if (parent != null)
                {
                    var f =
                        await parent.TryGetItemAsync(CommonSharedStrings.FolderJpg) as StorageFile ??
                        await parent.TryGetItemAsync(CommonSharedStrings.CoverJpg) as StorageFile ??
                        await parent.TryGetItemAsync(CommonSharedStrings.FolderPng) as StorageFile ??
                        await parent.TryGetItemAsync(CommonSharedStrings.CoverPng) as StorageFile;
                    if (f != null)
                    {
                        return await f.OpenReadAsync();
                    }
                }
            }
            return null;
        }

        static private async Task<BitmapImage> RetrieveStorageFileAsync(IStorageFile file, bool parentFolder)
        {
            BitmapImage img = null;
            using (var coverStream = await CoverExtractor.GetCoverStreamFromFileAsync(file))
            {
                if (coverStream != null)
                    img = await CreateBitmapFromStreamAsync(coverStream);
            }

            if (img == null && file is IStorageItem2 && parentFolder)
            {
                // The file itself does not contain a cover image.
                // Search in parent folder instead.
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
                            img = await CreateBitmapFromStreamAsync(s);
                    }
                }
            }

            // Cache even if null.
            _bitmapDiskCache.TryAdd(file.Path, img);
            return img;
        }

        static public async Task<BitmapImage> RetrieveOnlineAsync(string cacheName, string imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                // Temporarily disable cache for this file.
                _bitmapDiskCache.TryAdd(cacheName, null);
                return null;
            }
            try
            {
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(new Uri(imageUrl)))
                    {
                        response.EnsureSuccessStatusCode();
                        response.Content.Headers.TryGetValues("Content-Length", out var ctlength);
                        var lengthValue = ctlength.FirstOrDefault();

                        if (lengthValue == null ||
                            !ulong.TryParse(lengthValue, out ulong length))
                        {
                            length = 0;
                        }

                        if (length > MaxOnlineImageSize)
                        {
                            _bitmapDiskCache.TryAdd(cacheName, null);
                            return null;
                        }

                        var content = await response.Content.ReadAsByteArrayAsync();

                        if (content.Length < (int)length)
                        {
                            // Incomplete download, drop.
                            _bitmapDiskCache.TryAdd(cacheName, null);
                            return null;
                        }

                        var cacheFile = await localCache.CreateFileAsync(
                            cacheName,
                            CreationCollisionOption.ReplaceExisting);
                        using (var fileStream = await cacheFile.OpenStreamForWriteAsync())
                        {
                            await fileStream.WriteAsync(content, 0, content.Length);
                        }

                        var image = await CreateBitmapFromStreamAsync(
                            new MemoryStream(content).AsRandomAccessStream());
                        _bitmapDiskCache.AddOrUpdate(cacheFile.Path, image);
                        return image;
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        static public async Task<Tuple<BitmapImage, bool>> TryRetrieveOnlineCacheAsync(string cacheName)
        {
            try
            {
                var cachePath = Path.Combine(localCache.Path, cacheName);

                if (_bitmapDiskCache.TryGetValue(cachePath, out BitmapImage img))
                {
                    // Memory cache hit
                    return new Tuple<BitmapImage, bool>(img, true);
                }
                else
                {
                    var file = await localCache.TryGetItemAsync(cacheName) as StorageFile;
                    if (file == null) return CacheMiss;

                    using (var fs = await file.OpenReadAsync())
                    {
                        if (fs.Size == 0)
                        {
                            _bitmapDiskCache.TryAdd(cachePath, null);
                            return CacheDisabled; // Empty file stub
                        }

                        img = await CreateBitmapFromStreamAsync(fs);
                        if (img != null)
                        {
                            _bitmapDiskCache.AddOrUpdate(cachePath, img);
                            return new Tuple<BitmapImage, bool>(img, true); // Cache found
                        }
                    }
                }
            }
            catch { }
            return CacheDisabled; // Decode error
        }

        static public async Task<BitmapImage> RetrieveFromDiskAsync(string path)
        {
            if (_bitmapDiskCache.TryGetValue(path, out BitmapImage img))
            {
                return img; // Memory cache hit
            }
            else
            {
                // Try reading from disk
                var file = await NativeMethods.GetStorageFileFromPathAsync(path);

                if (file == null)
                {
                    _bitmapDiskCache.TryAdd(path, null);
                    return null;
                }
                try
                {
                    return await RetrieveStorageFileAsync(file, true);
                }
                catch { }
                return null;
            }
        }
    }
}
