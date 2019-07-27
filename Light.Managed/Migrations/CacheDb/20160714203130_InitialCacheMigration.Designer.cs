#if ENABLE_STAGING
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Light.Managed.Database;

namespace Light.Managed.Migrations.CacheDb
{
    [DbContext(typeof(CacheDbContext))]
    [Migration("20160714203130_InitialCacheMigration")]
    partial class InitialCacheMigration
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("Light.Managed.Database.Entities.CacheSet", b =>
                {
                    b.Property<string>("Id");

                    b.Property<string>("Path")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("CacheSets");
                });
        }
    }
}
#endif
