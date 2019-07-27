using Windows.Foundation.Metadata;
using Windows.System.Profile;

namespace Light.Common
{
    /// <summary>
    /// Enum of platforms.
    /// </summary>
    public enum Platform
    {
        Unknown = 0x00,
        WindowsDesktop = 0x01,
        WindowsMobile = 0x02,
        WindowsIoT = 0x03,
        MicrosoftHoloLens = 0x04,
        MicrosoftSurfaceHub = 0x05
    }

    /// <summary>
    /// Helper class for platform/OS diversity handling.
    /// </summary>
    public static class PlatformInfo
    {
        /// <summary>
        /// Indicates the current runtime platform.
        /// </summary>
        public static Platform CurrentPlatform;

        /// <summary>
        /// Indicates whether the current OS release is Windows 10 rs1+.
        /// </summary>
        public static bool IsRedstoneRelease =>
            ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3);

        static PlatformInfo()
        {
            // Query API Presence
            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                case CommonSharedStrings.WindowsDesktopId:
                    CurrentPlatform = Platform.WindowsDesktop;
                    break;
                case CommonSharedStrings.WindowsMobileId:
                    CurrentPlatform = Platform.WindowsMobile;
                    break;
                case CommonSharedStrings.WindowsTeamId:
                    CurrentPlatform = Platform.MicrosoftSurfaceHub;
                    break;
                case CommonSharedStrings.WindowsIoTId:
                    CurrentPlatform = Platform.WindowsIoT;
                    break;
                case CommonSharedStrings.WindowsHolographicId:
                    CurrentPlatform = Platform.MicrosoftHoloLens;
                    break;
                default:
                    CurrentPlatform = Platform.Unknown;
                    break;
            }
        }
    }
}
