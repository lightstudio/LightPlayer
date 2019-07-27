using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Light.Managed.Database.Constant;
using Light.Managed.Database;
#if ENABLE_STAGING
using Light.Managed.Extension;
#endif
using Light.Managed.Library;
using Light.Managed.Constants;
using Light.Managed.Settings;
using Light.Managed.Migrations;
using System.Threading.Tasks;
using WindowsRuntimeEtwLoggerProvider.Internals;

namespace Light
{
    /// <summary>
    /// Class contains application services for dependency injection.
    /// </summary>
    public class ApplicationServiceBase
    {
        private static ApplicationServiceBase _srvBase;
        public static ApplicationServiceBase App
        {
            get
            {
                if (_srvBase == null)
                {
                    _srvBase = new ApplicationServiceBase();
                }

                return _srvBase;
            }
        }

        private IServiceCollection m_serviceCollection;
        private IServiceProvider m_serviceProvider;
        private IServiceScopeFactory m_scopedFactory;

        private bool m_isConfigured;

        /// <summary>
        /// Implementation of <see cref="IServiceProvider"/>.
        /// </summary>
        public IServiceProvider ApplicationServices => m_serviceProvider;

        /// <summary>
        /// Root object of configuration.
        /// </summary>
        public IConfigurationRoot Configuration { get; }

        /// <summary>
        /// Current Windows ETW channel ID.
        /// </summary>
        public Guid EtwChannelId => LoggingChannelSingleton.Instance.ChannelId;

        /// <summary>
        /// Class constructor that creates instance of <see cref="ApplicationServiceBase"/>.
        /// </summary>
        public ApplicationServiceBase()
        {
            // Load configuration (currently, hard-coded).

            var builder = new ConfigurationBuilder()
                .AddInMemoryCollection(new List<KeyValuePair<string, string>>
                {
#if DEBUG
                    new KeyValuePair<string, string>("Logging:IncludeScopes", "False"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Default", "Debug"),
                    new KeyValuePair<string, string>("Logging:LogLevel:System", "Information"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Microsoft", "Information")
#else
                    new KeyValuePair<string, string>("Logging:IncludeScopes", "False"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Default", "Error"),
                    new KeyValuePair<string, string>("Logging:LogLevel:System", "Error"),
                    new KeyValuePair<string, string>("Logging:LogLevel:Microsoft", "Error")
#endif
                })
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            m_serviceCollection = new ServiceCollection();
            ConfigureServices(m_serviceCollection);
            m_serviceProvider = m_serviceCollection.BuildServiceProvider();
            m_isConfigured = false;
        }

        /// <summary>
        /// Configure application services asynchronously.
        /// </summary>
        public async Task ConfigureServicesAsync()
        {
            await ConfigureAsync(m_serviceProvider, m_serviceProvider.GetService<ILoggerFactory>());
            m_scopedFactory = m_serviceProvider.GetRequiredService<IServiceScopeFactory>();
        }

        /// <summary>
        /// Method that configures all foundation services.
        /// </summary>
        /// <param name="services">The collection of runtime services.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to add services to the container.
        ///  For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        /// </remarks>
        private void ConfigureServices(IServiceCollection services)
        {
            // Add options.
            services.AddOptions();

            // Add logging
            services.AddLogging();

            // Add library database service.
            services.AddDbContext<MedialibraryDbContext>(
                option => option.UseSqlite(ResolveDatabaseOption(DatabaseConstants.DbFileName)));

#if ENABLE_STAGING
            // Add cache database service.
            services.AddDbContext<CacheDbContext>(
                option => option.UseSqlite(ResolveDatabaseOption(DatabaseConstants.CacheDbFileName)));

            // Add extension database service.
            services.AddDbContext<ExtensionDbContext>(
                option => option.UseSqlite(ResolveDatabaseOption(DatabaseConstants.ExtensionFileName)));

            // Add extension management and database worker.
            services.AddScoped<ExtensionDatabaseWorker>();
            services.AddScoped<ExtensionManager>();
#endif

            // Add file index service.
            services.AddScoped<FileIndexer>();
        }

        /// <summary>
        /// Helper method for resolving database path and build option.
        /// </summary>
        /// <param name="databaseFileName">Name of the database.</param>
        /// <returns>Resolved database connection string.</returns>
        private string ResolveDatabaseOption(string databaseFileName)
        {
            var databaseSrc = string.Empty;
#if EFCORE_MIGRATION
            databaseSrc = $"Data source=MigrationStub.sqlite";
#else
            var dbPath = ResolveDatabasePath(databaseFileName);
            databaseSrc = $"Data source={dbPath}";
#endif

            return databaseSrc;
        }

        /// <summary>
        /// Resolves database actual filesystem path.
        /// </summary>
        /// <param name="strDatabaseFileName">Name of the database.</param>
        /// <returns>Resolved actual database path.</returns>
        public static string ResolveDatabasePath(string strDatabaseFileName) 
            => Path.Combine(ApplicationData.Current.LocalFolder.Path, strDatabaseFileName);

        /// <summary>
        /// Method that configures the application (e.g. database migration).
        /// </summary>
        /// <param name="serviceProvider">Implementation of <see cref="IServiceProvider"/>.</param>
        /// <param name="loggerFactory">Implementation of <see cref="ILoggerFactory"/>.</param>
        /// <remarks>
        /// This method gets called by the runtime. Use this method to configure the app pipeline.
        /// </remarks>
        private async Task ConfigureAsync(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
        {
            // Configure logging in Debug configuration.
#if DEBUG
            loggerFactory.AddDebug();
#endif
            // Always add ETW logging
            loggerFactory.AddEtwChannel();

            // Migrate database if necessary.
            if (SettingsManager.Instance.GetValue<int>(LibraryConstants.DatabaseMigrationLevel) 
                < LibraryConstants.CurrentMigrationLevel)
            {
                using (var scope = serviceProvider.GetService<IServiceScopeFactory>().CreateScope())
                using (var dbContext = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
#if ENABLE_STAGING
            using (var cacheDbContext = scope.ServiceProvider.GetRequiredService<CacheDbContext>())
            using (var extDbContext = scope.ServiceProvider.GetRequiredService<ExtensionDbContext>())
#endif
                {
                    await dbContext.Database.MigrateAsync();

#if ENABLE_STAGING
                await cacheDbContext.Database.MigrateAsync();
                await extDbContext.Database.MigrateAsync();
#endif
                }

                // Set migration flag
                SettingsManager.Instance.SetValue(LibraryConstants.CurrentMigrationLevel,
                    LibraryConstants.DatabaseMigrationLevel);
            }

            m_isConfigured = true;
        }

        /// <summary>
        /// Get an instance of <see cref="IServiceScope"/>.
        /// </summary>
        /// <returns></returns>
        public IServiceScope GetScope()
        {
            if (!m_isConfigured) throw new InvalidOperationException("Services have not been initialized.");
            return m_scopedFactory.CreateScope();
        }
    }
}
