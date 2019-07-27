using Windows.Networking.Connectivity;

namespace Light.Managed.Tools
{
    /// <summary>
    /// Utils to detect Internet connectivity on devices except Xbox.
    /// </summary>
    /// <remarks>Xbox do not support NetworkInformation class.</remarks>
    public static class InternetConnectivityDetector
    {
        /// <summary>
        /// Return a value, indicates whether there's Internet connection currently or not.
        /// </summary>
        public static bool HasInternetConnection
        {
            get
            {
                var inetProfile = NetworkInformation.GetInternetConnectionProfile();
                // Validate existence and connectivity
                return inetProfile?.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
            }
        }
    }
}
