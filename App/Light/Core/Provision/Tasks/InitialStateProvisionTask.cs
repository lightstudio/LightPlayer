using System;
using System.Threading.Tasks;
using Light.Managed.Tools;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Initial view state provision task.
    /// </summary>
    public class InitialStateProvisionTask : IProvisionTask
    {
        public string TaskName => "Initial State Provision Task";
        public Version RequiredVersion => new Version(1, 0, 0, 1);

        public Task ProvisionAsync()
        {
            // Set initial mode.
            PageStateManger.SetPageStatus("FrameView.Playback", "CycleAllMenuItem");
            NowPlayingStateManager.Volume = NowPlayingStateManager.DefaultVolume;

            return Task.FromResult(0);
        }
    }
}
