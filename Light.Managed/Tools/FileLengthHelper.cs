// ReSharper disable PossibleLossOfFraction

namespace Light.Managed.Tools
{
    /// <summary>
    /// Helper class for file length.
    /// </summary>
    public static class FileLengthHelper
    {
        #region Unit conversion
        private const ulong KibiBytes = 1024UL;
        private const ulong MebiBytes = KibiBytes * 1024UL;
        private const ulong GibiBytes = MebiBytes * 1024UL;
        private const ulong TebiBytes = GibiBytes * 1024UL;
        private const ulong PebiBytes = TebiBytes * 1024UL;
        private const ulong ExbiBytes = PebiBytes * 1024UL;
        #endregion

        /// <summary>
        /// Get formatted file size string.
        /// </summary>
        /// <param name="size">File size, in bytes.</param>
        /// <returns>An formatted string.</returns>
        public static string GetFormattedFileLengthString(this ulong size)
        {
            double displaySize;
            string suffix;

            if (size > ExbiBytes)
            {
                displaySize = size / ExbiBytes;
                suffix = "EiB";
            }
            else if (size > PebiBytes)
            {
                displaySize = size / PebiBytes;
                suffix = "PiB";
            }
            else if (size > TebiBytes)
            {
                displaySize = size / TebiBytes;
                suffix = "TiB";
            }
            else if (size > GibiBytes)
            {
                displaySize = size / GibiBytes;
                suffix = "GiB";
            }
            else if (size > MebiBytes)
            {
                displaySize = size / MebiBytes;
                suffix = "MiB";
            }
            else if (size > KibiBytes)
            {
                displaySize = size / KibiBytes;
                suffix = "KiB";
            }
            else
            {
                return string.Format($"{size} B");
            }
            return string.Format($"{displaySize:0.00} {suffix} ({size:0,0} bytes)");
        }
    }
}
