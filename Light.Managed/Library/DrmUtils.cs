using System;
using System.Threading.Tasks;
using Windows.Storage;
using Light.Managed.Tools;

namespace Light.Managed.Library
{
    public static class DrmUtils
    {
        public static async Task<bool> RetrieveDrmStatus(StorageFile file)
        {
            try
            {
                var prop = await file.Properties.RetrievePropertiesAsync(new [] {"System.DRM.IsProtected"});
                if (prop.TryGetValue("System.DRM.IsProtected", out object value))
                {
                    return (bool) value;
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }

            return false;
        }
    }
}
