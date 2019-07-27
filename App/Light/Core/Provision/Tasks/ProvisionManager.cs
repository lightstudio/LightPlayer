using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Light.Core.Provision.Tasks
{
    /// <summary>
    /// Manages fixed or on-demand provision tasks.
    /// </summary>
    public class ProvisionManager
    {
        private readonly IPropertySet m_provisionHistory;
        private readonly IPropertySet m_podQueue;
        private readonly IReadOnlyList<IProvisionTask> m_provDef;

        /// <summary>
        /// Initializes new instance of the <see cref="ProvisionManager"/> class.
        /// </summary>
        public ProvisionManager()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(nameof(ProvisionManager),
                ApplicationDataCreateDisposition.Always);
            m_provisionHistory = container.Values;

            var podContainer = ApplicationData.Current.LocalSettings.CreateContainer(nameof(m_podQueue), 
                ApplicationDataCreateDisposition.Always);
            m_podQueue = podContainer.Values;

            m_provDef = new ProvisionDefinition();
        }

        /// <summary>
        /// Indicates whether provision is required.
        /// </summary>
        public bool IsProvisionRequired
        {
            get
            {
                if (m_podQueue.Count > 0) return true;

                foreach (var def in m_provDef)
                {
                    if (m_provisionHistory.ContainsKey(def.TaskName))
                    {
                        var currentVersion = Version.Parse((string) m_provisionHistory[def.TaskName]);
                        if (currentVersion < def.RequiredVersion)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Run all required provision tasks asynchronously.
        /// </summary>
        /// <returns>Task that represents the asynchronous provision operation.</returns>
        public async Task RunProvisionTasksAsync()
        {
            foreach (var def in m_provDef)
            {
                var required = true;
                if (m_provisionHistory.ContainsKey(def.TaskName))
                {
                    var currentVersion = Version.Parse((string)m_provisionHistory[def.TaskName]);
                    required = currentVersion >= def.RequiredVersion;
                }

                if (required) await RunSingleProvisionTaskAsync(def);
            }

            foreach (var task in m_podQueue)
            {
                var pTask = (IProvisionTask) Activator.CreateInstance(Type.GetType((string)task.Value));
                await pTask.ProvisionAsync();
            }

            m_podQueue.Clear();
        }

        /// <summary>
        /// Execute single provision task asynchronously.
        /// </summary>
        /// <param name="task">Type of the provision task to be performed.</param>
        /// <returns>Task that represents the asynchronous provision operation.</returns>
        private async Task RunSingleProvisionTaskAsync(IProvisionTask task)
        {
            if (task == null) throw new ArgumentException(nameof(task));
            await task.ProvisionAsync();

            // Update record
            if (m_provisionHistory.ContainsKey(task.TaskName))
            {
                m_provisionHistory[task.TaskName] = task.RequiredVersion.ToString();
            }
            else
            {
                m_provisionHistory.Add(task.TaskName, task.RequiredVersion.ToString());
            }
        }

        /// <summary>
        /// Enqueue an on-demand provision task on next launch.
        /// </summary>
        /// <typeparam name="T">Type of the on-demand provision task.</typeparam>
        public void SetTaskOnNextLaunch<T>() where T: IProvisionTask
        {
            m_podQueue.Add(Guid.NewGuid().ToString(), typeof(T).AssemblyQualifiedName);
        }
    }
}
