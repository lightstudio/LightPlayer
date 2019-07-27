#if ENABLE_STAGING
using Microsoft.EntityFrameworkCore.Migrations;

namespace Light.Managed.Migrations.CacheDb
{
    public partial class NxtCacheMgmt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CacheSets");

            migrationBuilder.CreateTable(
                name: "CacheSets",
                columns: table => new
                {
                    Key = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CacheSets", x => x.Key);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CacheSets");

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
    }
}
#endif