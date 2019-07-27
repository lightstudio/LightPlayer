#if ENABLE_STAGING
using Light.Managed.Extension.Model;
using Microsoft.EntityFrameworkCore;

namespace Light.Managed.Extension
{
    public class ExtensionDbContext : DbContext
    {
        public virtual DbSet<ExtPackage> Packages { get; set; }
        public virtual DbSet<FormatTable> Formats { get; set; }

        /// <summary>
        /// Class constructor that creates instance of <see cref="ExtensionDbContext"/> without caching.
        /// </summary>
        /// <param name="options">Instance of <see cref="DbContextOptions"/>.</param>
        public ExtensionDbContext(DbContextOptions<ExtensionDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ExtPackage>()
                .Property(p => p.Name)
                .IsRequired();

            modelBuilder.Entity<ExtPackage>()
                .Property(p => p.EntryPoint)
                .IsRequired();

            modelBuilder.Entity<FormatTable>()
                .Property(p => p.PluginId)
                .IsRequired();

            modelBuilder.Entity<FormatTable>()
                .Property(p => p.Format)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}
#endif