using Windows.Foundation.Metadata;

namespace Light.Managed.Constants
{
    /// <summary>
    /// Constant values of library.
    /// </summary>
    static class LibraryConstants
    {
        /// <summary>
        /// Migration level that controls database migration.
        /// DO change this after creating new migration.
        /// </summary>
        public const int CurrentMigrationLevel = 10;

        /// <summary>
        /// Key of the database migration level that will be stored in <see cref="Light.Managed.Settings.SettingsManager"/>.
        /// </summary>
        public const string DatabaseMigrationLevel = nameof(DatabaseMigrationLevel);
    }
}
