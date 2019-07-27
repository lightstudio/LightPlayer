using System;
using System.Threading.Tasks;
using Light.Managed.Background;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Background registration provision task.
    /// </summary>
    public class BackgroundProvisionTask : IProvisionTask
    {
        public string TaskName => "Background Provision Task";
        public Version RequiredVersion => new Version(1, 0, 0, 0);

        public Task ProvisionAsync()
        {
            // Provision Background Task.
            BackgroundTaskRegisterationHelper.Register();

            return Task.FromResult(0);
        }
    }
}
