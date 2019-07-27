using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.ExpressionTranslators.Internal;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Light.Managed.Database
{
    /// <summary>
    /// Library context.
    /// </summary>
    public class MedialibraryDbContext : DbContext
    {
        /// <summary>
        /// Set of albums.
        /// </summary>
        public DbSet<DbAlbum> Albums { get; set; }

        /// <summary>
        /// Set of artists.
        /// </summary>
        public DbSet<DbArtist> Artists { get; set; }

        /// <summary>
        /// Set of media files.
        /// </summary>
        public DbSet<DbMediaFile> MediaFiles { get; set; }

        /// <summary>
        /// Set of playback history.
        /// </summary>
        public DbSet<DbPlaybackHistory> PlaybackHistory { get; set; }

        /// <summary>
        /// Class constructor that creates instance of <see cref="MedialibraryDbContext"/> without caching.
        /// </summary>
        /// <param name="options">Instance of <see cref="DbContextOptions"/>.</param>
        public MedialibraryDbContext(DbContextOptions<MedialibraryDbContext> options) : base(options)
        {
        }

#if EFCORE_MIGRATION
        public MedialibraryDbContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite($"Data source=MigrationStub.sqlite");
        }
#else
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.ReplaceService<SqliteCompositeMethodCallTranslator, CustomSqliteCompositeMethodCallTranslator>();
            base.OnConfiguring(optionsBuilder);
        }
#endif

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbMediaFile>()
                .Ignore(p => p.IsExternal);

            modelBuilder.Entity<DbMediaFile>()
              .Ignore(p => p.ExternalFileId);

            modelBuilder.Entity<DbMediaFile>()
             .Ignore(p => p.ExternalFileId);

            modelBuilder.Entity<DbPlaybackHistory>()
                .HasIndex(p => p.PlaybackTime);

            base.OnModelCreating(modelBuilder);
        }
    }
}
