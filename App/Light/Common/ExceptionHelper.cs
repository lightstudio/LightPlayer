using System;

namespace Light.Common
{
    internal static class ThrowHelper
    {
        public static void ThrowNotSupportedDeviceException()
        {
            throw new NotSupportedException(CommonSharedStrings.DeviceNotSupportedString);
        }
    }
}
