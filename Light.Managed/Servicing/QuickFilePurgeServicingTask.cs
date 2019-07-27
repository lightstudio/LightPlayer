using Light.Managed.Database.Constant;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace Light.Managed.Servicing
{
    /// <summary>
    /// Single servicing task that purges related application data.
    /// </summary>
    [MaximumQualifiedRevision(1484)]
    public sealed class QuickFilePurgeServicingTask : IServicingTask
    {
        private static string[] m_additionalFiles = { "NowPlaying.json", "NowPlayingIndex" };

        /// <inheritdoc />
        public async Task RunAsync()
        {
            var strLegacyDatabasePath = ApplicationServiceBase.ResolveDatabasePath(DatabaseConstants.Legacyv4DbFileName);
            try
            {
                var siDatabase = await StorageFile.GetFileFromPathAsync(strLegacyDatabasePath);
                if (siDatabase == null) return;
                // Remove legacy database
                await siDatabase.DeleteAsync(StorageDeleteOption.PermanentDelete);
                // Remove additional files, if necessary
                foreach (var strFileName in m_additionalFiles) await RemoveCacheFileIfExistsAsync(strFileName);
            }
            catch (FileNotFoundException)
            {
                // Ignore, the servicing task is not applicable
            }
        }

        /// <summary>
        /// Attempts to remove given file if it exists in local cache directory.
        /// </summary>
        /// <param name="strFileName">File name.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        private static async Task RemoveCacheFileIfExistsAsync(string strFileName)
        {
            if (strFileName == null) throw new ArgumentNullException(nameof(strFileName));
            var file = await ApplicationData.Current.LocalCacheFolder.TryGetItemAsync(strFileName);
            if (file != null) await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
        }
    }
}
