using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Light.Managed.Database;

namespace Light.Managed.Migrations
{
    [DbContext(typeof(MedialibraryDbContext))]
    [Migration("20170423103227_FreshStart1704")]
    partial class FreshStart1704
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.1");

            modelBuilder.Entity("Light.Managed.Database.Entities.DbAlbum", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Artist");

                    b.Property<string>("CoverPath");

                    b.Property<DateTimeOffset>("DatabaseItemAddedDate");

                    b.Property<string>("Date");

                    b.Property<int>("FileCount");

                    b.Property<string>("FirstFileInAlbum");

                    b.Property<string>("Genre");

                    b.Property<int>("GenreId");

                    b.Property<string>("Intro");

                    b.Property<int?>("RelatedArtistId");

                    b.Property<string>("Title");

                    b.HasKey("Id");

                    b.HasIndex("RelatedArtistId");

                    b.ToTable("Albums");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbArtist", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<int>("AlbumCount");

                    b.Property<DateTimeOffset>("DatabaseItemAddedDate");

                    b.Property<int>("FileCount");

                    b.Property<string>("ImagePath");

                    b.Property<string>("Intro");

                    b.Property<string>("Name");

                    b.HasKey("Id");

                    b.ToTable("Artists");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbMediaFile", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("Album");

                    b.Property<string>("AlbumArtist");

                    b.Property<string>("Artist");

                    b.Property<string>("Comments");

                    b.Property<string>("Composer");

                    b.Property<string>("Copyright");

                    b.Property<string>("CoverHashId");

                    b.Property<DateTimeOffset>("DatabaseItemAddedDate");

                    b.Property<string>("Date");

                    b.Property<string>("Description");

                    b.Property<string>("DiscNumber");

                    b.Property<TimeSpan>("Duration");

                    b.Property<DateTimeOffset>("FileLastModifiedDate");

                    b.Property<string>("Genre");

                    b.Property<string>("Grouping");

                    b.Property<string>("Path");

                    b.Property<string>("Performer");

                    b.Property<int?>("RelatedAlbumId");

                    b.Property<int?>("RelatedArtistId");

                    b.Property<int?>("StartTime");

                    b.Property<string>("Title");

                    b.Property<string>("TotalDiscs");

                    b.Property<string>("TotalTracks");

                    b.Property<string>("TrackNumber");

                    b.HasKey("Id");

                    b.HasIndex("RelatedAlbumId");

                    b.HasIndex("RelatedArtistId");

                    b.ToTable("MediaFiles");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbPlaybackHistory", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("FilePath");

                    b.Property<DateTimeOffset>("PlaybackTime");

                    b.Property<int?>("RelatedMediaFileId");

                    b.HasKey("Id");

                    b.HasIndex("PlaybackTime");

                    b.HasIndex("RelatedMediaFileId");

                    b.ToTable("PlaybackHistory");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbAlbum", b =>
                {
                    b.HasOne("Light.Managed.Database.Entities.DbArtist", "RelatedArtist")
                        .WithMany("Albums")
                        .HasForeignKey("RelatedArtistId");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbMediaFile", b =>
                {
                    b.HasOne("Light.Managed.Database.Entities.DbAlbum", "RelatedAlbum")
                        .WithMany("MediaFiles")
                        .HasForeignKey("RelatedAlbumId");

                    b.HasOne("Light.Managed.Database.Entities.DbArtist", "RelatedArtist")
                        .WithMany("MediaFiles")
                        .HasForeignKey("RelatedArtistId");
                });

            modelBuilder.Entity("Light.Managed.Database.Entities.DbPlaybackHistory", b =>
                {
                    b.HasOne("Light.Managed.Database.Entities.DbMediaFile", "RelatedMediaFile")
                        .WithMany()
                        .HasForeignKey("RelatedMediaFileId");
                });
        }
    }
}
