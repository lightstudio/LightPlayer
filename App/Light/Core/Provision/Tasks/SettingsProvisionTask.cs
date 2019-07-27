using System;
using System.Threading.Tasks;
using Windows.Storage;
using Light.Managed.Settings;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Initial task for performing settings provision.
    /// </summary>
    public class SettingsProvisionTask : IProvisionTask
    {
        public string TaskName => "Settings Provision Task";
        public Version RequiredVersion => new Version(4, 1, 0 ,0);

        public async Task ProvisionAsync()
        {
            await
                SettingsManager.Instance.ApplyProvisionFileAsync(
                    await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///SettingsProvision/SettingsDefault.xml")));
        }
    }
}
