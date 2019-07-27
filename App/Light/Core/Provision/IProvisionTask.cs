using System;
using System.Threading.Tasks;

namespace Light.Core.Provision
{
    /// <summary>
    /// General provision task interface.
    /// </summary>
    public interface IProvisionTask
    {
        /// <summary>
        /// Task name.
        /// </summary>
        string TaskName { get; }

        /// <summary>
        /// Task required min version.
        /// </summary>
        Version RequiredVersion { get; }

        /// <summary>
        /// Method to perform provision task.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        Task ProvisionAsync();
    }
}
