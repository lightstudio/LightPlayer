#if ENABLE_STAGING
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Light.Managed.Migrations.CacheDb
{
    public partial class InitialCacheMigration : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CacheSets",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Path = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheSets", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CacheSets");
        }
    }
}
#endif
