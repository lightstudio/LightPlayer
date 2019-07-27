using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Light.Managed.Database.Native
{
    public class NativeMethods
    {
        [DllImport("Light.dll")]
        public static extern int GetAlbumCoverFromStream(
            IRandomAccessStream stream,
            out IRandomAccessStream outStream);
        [DllImport("Light.dll")]
        public static extern int GetMediaInfoFromStream(
             IRandomAccessStream stream,
             out IMediaInfo outMediaInfo);

        [DllImport("Light.dll")]
        public static extern int GetMediaInfoFromFilePath(
            [MarshalAs(UnmanagedType.LPWStr)] string filePath,
            out IMediaInfo outMediaInfo);

        [DllImport("Light.dll")]
        public static extern int GetStorageFileFromPath(
             [MarshalAs(UnmanagedType.LPWStr)]
             string path,
             out IStorageFile file);

        [DllImport("Light.dll")]
        private static extern int GetStorageFileFromPathAsync(
            [MarshalAs(UnmanagedType.LPWStr)]
            string path,
            out IAsyncOperation<IStorageFile> op);

        [DllImport("Light")]
        private static extern int GetStorageFolderFromPathAsync(
            [MarshalAs(UnmanagedType.LPWStr)]
            string path,
            out IAsyncOperation<IStorageFolder> op);

        public static IAsyncOperation<IStorageFile> GetStorageFileFromPathAsync(
            string path)
        {
            IAsyncOperation<IStorageFile> op;
            var result = GetStorageFileFromPathAsync(path, out op);
            return op;
        }

        public static IAsyncOperation<IStorageFolder> GetStorageFolderFromPathAsync(
            string path)
        {
            IAsyncOperation<IStorageFolder> op;
            var result = GetStorageFolderFromPathAsync(path, out op);
            return op;
        }
        
        [DllImport("Light.dll")]
        public static extern int GetStorageFolderFromPath(
             [MarshalAs(UnmanagedType.LPWStr)]
             string path,
             out IStorageFolder folder);

        [DllImport("Light.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool ShouldUseWinRT(
             [MarshalAs(UnmanagedType.LPWStr)] string path);

        [DllImport("Light.dll")]
        public static extern int InitializeFfmpeg();
    }

    internal enum HResultEnum
    {
        S_OK = 0
    }
}
