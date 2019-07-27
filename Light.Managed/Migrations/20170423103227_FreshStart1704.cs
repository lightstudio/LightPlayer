using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Light.Managed.Migrations
{
    public partial class FreshStart1704 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Artists",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AlbumCount = table.Column<int>(nullable: false),
                    DatabaseItemAddedDate = table.Column<DateTimeOffset>(nullable: false),
                    FileCount = table.Column<int>(nullable: false),
                    ImagePath = table.Column<string>(nullable: true),
                    Intro = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Artists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Artist = table.Column<string>(nullable: true),
                    CoverPath = table.Column<string>(nullable: true),
                    DatabaseItemAddedDate = table.Column<DateTimeOffset>(nullable: false),
                    Date = table.Column<string>(nullable: true),
                    FileCount = table.Column<int>(nullable: false),
                    FirstFileInAlbum = table.Column<string>(nullable: true),
                    Genre = table.Column<string>(nullable: true),
                    GenreId = table.Column<int>(nullable: false),
                    Intro = table.Column<string>(nullable: true),
                    RelatedArtistId = table.Column<int>(nullable: true),
                    Title = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Albums_Artists_RelatedArtistId",
                        column: x => x.RelatedArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MediaFiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Album = table.Column<string>(nullable: true),
                    AlbumArtist = table.Column<string>(nullable: true),
                    Artist = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    Composer = table.Column<string>(nullable: true),
                    Copyright = table.Column<string>(nullable: true),
                    CoverHashId = table.Column<string>(nullable: true),
                    DatabaseItemAddedDate = table.Column<DateTimeOffset>(nullable: false),
                    Date = table.Column<string>(nullable: true),
                    Description = table.Column<string>(nullable: true),
                    DiscNumber = table.Column<string>(nullable: true),
                    Duration = table.Column<TimeSpan>(nullable: false),
                    FileLastModifiedDate = table.Column<DateTimeOffset>(nullable: false),
                    Genre = table.Column<string>(nullable: true),
                    Grouping = table.Column<string>(nullable: true),
                    Path = table.Column<string>(nullable: true),
                    Performer = table.Column<string>(nullable: true),
                    RelatedAlbumId = table.Column<int>(nullable: true),
                    RelatedArtistId = table.Column<int>(nullable: true),
                    StartTime = table.Column<int>(nullable: true),
                    Title = table.Column<string>(nullable: true),
                    TotalDiscs = table.Column<string>(nullable: true),
                    TotalTracks = table.Column<string>(nullable: true),
                    TrackNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MediaFiles_Albums_RelatedAlbumId",
                        column: x => x.RelatedAlbumId,
                        principalTable: "Albums",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MediaFiles_Artists_RelatedArtistId",
                        column: x => x.RelatedArtistId,
                        principalTable: "Artists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlaybackHistory",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FilePath = table.Column<string>(nullable: true),
                    PlaybackTime = table.Column<DateTimeOffset>(nullable: false),
                    RelatedMediaFileId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlaybackHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlaybackHistory_MediaFiles_RelatedMediaFileId",
                        column: x => x.RelatedMediaFileId,
                        principalTable: "MediaFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Albums_RelatedArtistId",
                table: "Albums",
                column: "RelatedArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_RelatedAlbumId",
                table: "MediaFiles",
                column: "RelatedAlbumId");

            migrationBuilder.CreateIndex(
                name: "IX_MediaFiles_RelatedArtistId",
                table: "MediaFiles",
                column: "RelatedArtistId");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackHistory_PlaybackTime",
                table: "PlaybackHistory",
                column: "PlaybackTime");

            migrationBuilder.CreateIndex(
                name: "IX_PlaybackHistory_RelatedMediaFileId",
                table: "PlaybackHistory",
                column: "RelatedMediaFileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlaybackHistory");

            migrationBuilder.DropTable(
                name: "MediaFiles");

            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "Artists");
        }
    }
}
