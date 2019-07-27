using System;

namespace Light.Managed.Library
{
    /// <summary>
    /// Exception indicates that file is DRM-protected.
    /// </summary>
    class DrmProtectedException : Exception
    {
        /// <summary>
        /// Initializes new instance of <see cref="DrmProtectedException"/>.
        /// </summary>
        public DrmProtectedException() : base(LocalizedStrings.DrmExceptionMessage)
        {
            // Do nothing
        }
    }
}
