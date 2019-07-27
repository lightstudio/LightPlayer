#if ENABLE_STAGING
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Light.Managed.Migrations.ExtensionDb
{
    public partial class InitialExtensionMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Packages",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    Arch = table.Column<int>(nullable: false),
                    EntryPoint = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    PackageId = table.Column<string>(nullable: true),
                    SecurityData = table.Column<string>(nullable: true),
                    SupportedFormats = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    Version = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Formats",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Autoincrement", true),
                    Format = table.Column<string>(nullable: false),
                    PluginId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Formats", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Packages");

            migrationBuilder.DropTable(
                name: "Formats");
        }
    }
}
#endif