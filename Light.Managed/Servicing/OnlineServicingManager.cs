using Light.Managed.Settings;
using System.Threading.Tasks;
using System.Reflection;
using Windows.ApplicationModel;

namespace Light.Managed.Servicing
{
    /// <summary>
    /// Provides servicing management services.
    /// </summary>
    public class OnlineServicingManager
    {
        private const string g_localBuildKey = "LocalBuild";

        /// <summary>
        /// Indicates whether online servicing is required.
        /// </summary>
        public static bool IsServicingRequired
        {
            get
            {
                var usMaxQualifiedRev = typeof(QuickFilePurgeServicingTask).GetTypeInfo().GetCustomAttribute<MaximumQualifiedRevisionAttribute>();
                return (StoredBuild < usMaxQualifiedRev.Revision);
            }
        }

        /// <summary>
        /// Stored local revision.
        /// </summary>
        private static ushort StoredBuild
        {
            get
            {
                if (SettingsManager.Instance.TryGetValue(g_localBuildKey, out object oCurrentRev))
                {
                    var uiCurrentRev = (ushort) oCurrentRev;
                    return uiCurrentRev;
                }

                return 0;
            }
        }

        /// <summary>
        /// Run servicing task asynchronously.
        /// </summary>
        /// <returns>Task represents the asynchronous operation.</returns>
        public static async Task RunAsync()
        {
            // Currently the only servicing task will nuke application directory.
            // We will expand it at later time
            if (!IsServicingRequired) return;

            var siTask = new QuickFilePurgeServicingTask();
            await siTask.RunAsync();
        }

        /// <summary>
        /// Commit current package version to local settings storage.
        /// </summary>
        public static void Commit()
        {
            var vPackage = Package.Current.Id.Version;

            if (StoredBuild >= vPackage.Build) return;
            SettingsManager.Instance.SetValue(vPackage.Build, g_localBuildKey);
        }
    }
}
