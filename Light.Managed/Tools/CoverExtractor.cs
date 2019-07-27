using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Light.Managed.Database.Native;

namespace Light.Managed.Tools
{
    public static class CoverExtractor
    {
        public static async Task<IRandomAccessStream> GetCoverStreamFromFileAsync(IStorageFile file)
        {
            using (var ras = await file.OpenAsync(FileAccessMode.Read))
            {
                IRandomAccessStream stream = null;
                await Task.Run(() =>
                {
                    NativeMethods.GetAlbumCoverFromStream(ras, out stream);
                });
                return stream;
            }
        }
    }
}
