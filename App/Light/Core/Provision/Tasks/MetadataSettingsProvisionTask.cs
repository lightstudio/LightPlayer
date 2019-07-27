using System;
using System.Threading.Tasks;
using Light.Managed.Online;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Metadata settings initial provision task.
    /// </summary>
    public class MetadataSettingsProvisionTask : IProvisionTask
    {
        public string TaskName => "Metadata Provider Settings Provision Task";
        public Version RequiredVersion => new Version(1, 0, 0, 0);
        public Task ProvisionAsync() => Task.Run(() =>
        {
            AggreatedOnlineMetadata.InitializeSettings();
        });
    }
}
