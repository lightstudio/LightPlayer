#if ENABLE_STAGING
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Light.Managed.Extension;

namespace Light.Managed.Migrations.ExtensionDb
{
    [DbContext(typeof(ExtensionDbContext))]
    partial class ExtensionDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.0.0-rtm-21431");

            modelBuilder.Entity("Light.Managed.Extension.Model.ExtPackage", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("Arch");

                    b.Property<string>("EntryPoint")
                        .IsRequired();

                    b.Property<string>("Name")
                        .IsRequired();

                    b.Property<string>("PackageId");

                    b.Property<string>("SecurityData");

                    b.Property<string>("SupportedFormats");

                    b.Property<int>("Type");

                    b.Property<int>("Version");

                    b.HasKey("Id");

                    b.ToTable("Packages");
                });

            modelBuilder.Entity("Light.Managed.Extension.Model.FormatTable", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Format")
                        .IsRequired();

                    b.Property<string>("PluginId")
                        .IsRequired();

                    b.HasKey("Id");

                    b.ToTable("Formats");
                });
        }
    }
}
#endif