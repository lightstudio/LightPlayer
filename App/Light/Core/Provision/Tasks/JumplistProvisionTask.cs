using System;
using System.Threading.Tasks;
using Light.Shell;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Jumplist provision task.
    /// Only supported on 10586+
    /// </summary>
    public class JumplistProvisionTask : IProvisionTask
    {
        public string TaskName => "Jumplist Provision Task";
        public Version RequiredVersion => new Version(1, 0, 0, 0);

        public async Task ProvisionAsync()
        {
            var jumplistHelper = new JumplistHelper();
            if (JumplistHelper.IsJumplistPresent)
            {
                await jumplistHelper.ProvisionJumplistAsync();
            }
        }
    }
}
