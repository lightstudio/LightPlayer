using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;

namespace Light.Managed.Library
{
    /// <summary>
    /// Accessor for library localized strings.
    /// </summary>
    static class LocalizedStrings
    {
        #region Resource Loader
        private static ResourceLoader m_resLoader;

        /// <summary>
        /// Instance of <see cref="ResourceLoader"/>.
        /// </summary>
        private static ResourceLoader RlInstance
        {
            get
            {
                if (m_resLoader == null)
                {
                    m_resLoader = ResourceLoader.GetForViewIndependentUse("Light.Managed/Resources");
                }

                return m_resLoader;
            }
        }

        /// <summary>
        /// Get certain localized string.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        static string GetString([CallerMemberName] string tag = null) => RlInstance.GetString(tag);

        #endregion

        public static string CueInvalid => GetString();
        public static string AudioFileAccessFailure => GetString();
        public static string AudioFileMetadataFailure => GetString();
        public static string AudioFileMetadataFailureExceptionMessage => GetString();
        public static string DrmExceptionMessage => GetString();
    }
}
