using System.Threading.Tasks;

namespace Light.Managed.Servicing
{
    /// <summary>
    /// Represents single servicing task.
    /// </summary>
    public interface IServicingTask
    {
        /// <summary>
        /// Run the servicing task asynchronously.
        /// </summary>
        /// <returns>Task represents the servicing operation.</returns>
        Task RunAsync();
    }
}
