#if ENABLE_STAGING
using Light.Managed.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Light.Managed.Database
{
    public class CacheDbContext : DbContext
    {
        public virtual DbSet<DbOnlineDataCache> CacheSets { get; set; }

        /// <summary>
        /// Class constructor that creates instance of <see cref="CacheDbContext"/> without caching.
        /// </summary>
        /// <param name="options">Instance of <see cref="DbContextOptions"/>.</param>
        public CacheDbContext(DbContextOptions<CacheDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DbOnlineDataCache>()
                .Property(p => p.Key)
                .IsRequired();

            modelBuilder.Entity<DbOnlineDataCache>()
                .Property(p => p.Value)
                .IsRequired();

            modelBuilder.Entity<DbOnlineDataCache>()
                .HasKey(p => p.Key);

            base.OnModelCreating(modelBuilder);
        }
    }
}
#endif