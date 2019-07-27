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
    [Migration("20160720204657_NxtCacheMgmt")]
    partial class NxtCacheMgmt
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("Light.Managed.Database.Entities.DbOnlineDataCache", b =>
                {
                    b.Property<string>("Key");

                    b.Property<string>("Value")
                        .IsRequired();

                    b.HasKey("Key");

                    b.ToTable("CacheSets");
                });
        }
    }
}
#endif
