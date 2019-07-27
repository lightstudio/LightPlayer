using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Light.CueIndex;

namespace Light.Managed.Database.Entities
{
    public class DbOnlineDataCache
    {
        [Key]
        public string Key { get; set; }
        public string Value { get; set; }
    }

    /// <summary>
    /// Standard media file entity class.
    /// </summary>
    public class DbMediaFile
    {
        [Key]
        public int Id { get; set; }

        public TimeSpan Duration { get; set; }

        public string TotalDiscs { get; set; }

        public string DiscNumber { get; set; }

        public string TotalTracks { get; set; }

        public string Description { get; set; }

        public string Copyright { get; set; }

        public string Comments { get; set; }

        public string Grouping { get; set; }

        public string Genre { get; set; }

        public string TrackNumber { get; set; }

        public string AlbumArtist { get; set; }

        public string Performer { get; set; }

        public string Composer { get; set; }

        public string Date { get; set; }

        public string Album { get; set; }

        public string Artist { get; set; }

        public string Title { get; set; }

        public string Path { get; set; }

        public DateTimeOffset DatabaseItemAddedDate { get; set; }

        public DateTimeOffset FileLastModifiedDate { get; set; }

        public string CoverHashId { get; set; }

        #region Medialibrary Utils
        public static DbMediaFile FromMediaInfo(IMediaInfo info, DateTimeOffset lastModified)
        {
            return new DbMediaFile
            {
                Album = info.Album.Trim(),
                AlbumArtist = info.AlbumArtist.Trim(),
                Artist = info.Artist.Trim(),
                Comments = info.Comments,
                Composer = info.Composer,
                Copyright = info.Copyright,
                Date = info.Date,
                Description = info.Description,
                DiscNumber = info.DiscNumber,
                Duration = info.Duration,
                Genre = info.Genre,
                Grouping = info.Grouping,
                Performer = info.Performer,
                Title = info.Title,
                TotalDiscs = info.TotalDiscs,
                TotalTracks = info.TotalTracks,
                TrackNumber = info.TrackNumber,
                FileLastModifiedDate = lastModified,
                DatabaseItemAddedDate = DateTimeOffset.UtcNow
            };
        }

        #endregion

        // Generation 2, redesigned to adapt Entity Framework
        public int? RelatedAlbumId { get; set; }

        [ForeignKey("RelatedAlbumId")]
        [JsonIgnore]
        public virtual DbAlbum RelatedAlbum { get; set; }

        public int? RelatedArtistId { get; set; }

        [ForeignKey("RelatedArtistId")]
        [JsonIgnore]
        public virtual DbArtist RelatedArtist { get; set; }

        [NotMapped]
        public bool IsExternal { get; set; }

        [NotMapped]
        public Guid ExternalFileId { get; set; }

        [NotMapped]
        [JsonIgnore]
        public ManagedAudioIndexCue MediaCue
        {
            get
            {
                if (StartTime.HasValue)
                {
                    return new ManagedAudioIndexCue
                    {
                        Duration = Duration,
                        StartTime = TimeSpan.FromMilliseconds(StartTime.Value)
                    };
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Start time in milliseconds.
        /// store cue track info.
        /// set to null when the media file is not associated to any CUE file.
        /// </summary>
        public int? StartTime { get; set; }

        /// <summary>
        /// Returns the string that can be used to uniquely identify a DbMediaFile entity.
        /// </summary>
        /// <returns>DbMediaFile identifier</returns>
        public override string ToString()
        {
            if (StartTime.HasValue)
            {
                return $"{Path.ToLower()}|{StartTime.Value}|{Duration.TotalMilliseconds}";
            }
            else
            {
                return Path.ToLower();
            }
        }
    }

    /// <summary>
    /// Standard album entity class.
    /// </summary>
    public class DbAlbum
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DbAlbum()
        {
            this.MediaFiles = new HashSet<DbMediaFile>();
        }

        [Key]
        public int Id { get; set; }

        public int GenreId { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Genre { get; set; }

        public string Date { get; set; }

        public string Intro { get; set; }

        public string CoverPath { get; set; }

        public int? RelatedArtistId { get; set; }

        public int FileCount { get; set; }

        public DateTimeOffset DatabaseItemAddedDate { get; set; }

        [ForeignKey("RelatedArtistId")]
        public DbArtist RelatedArtist { get; set; }

        // Generation 2, redesigned to adapt Entity Framework
        [InverseProperty("RelatedAlbum")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public ICollection<DbMediaFile> MediaFiles { get; set; }

        public string FirstFileInAlbum { get; set; }

        // Below are temp properties stored during scan.
        [NotMapped]
        internal int FirstFileDiscNumber { get; set; }
        [NotMapped]
        internal int FirstFileTrackNumber { get; set; }
    }

    /// <summary>
    /// Standard artist entity class.
    /// </summary>
    public class DbArtist
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        public DbArtist()
        {
            this.MediaFiles = new HashSet<DbMediaFile>();
            this.Albums = new HashSet<DbAlbum>();
        }

        [Key]
        public int Id { get; set; }

        public string Name { get; set; }

        public string ImagePath { get; set; }

        public string Intro { get; set; }

        public int FileCount { get; set; }

        public int AlbumCount { get; set; }

        public DateTimeOffset DatabaseItemAddedDate { get; set; }

        // Generation 2, redesigned to adapt Entity Framework
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [InverseProperty("RelatedArtist")]
        public virtual ICollection<DbMediaFile> MediaFiles { get; set; }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        [InverseProperty("RelatedArtist")]
        public virtual ICollection<DbAlbum> Albums { get; set; }
    }

    /// <summary>
    /// Playback history entity class
    /// </summary>
    public class DbPlaybackHistory
    {
        [Key]
        public int Id { get; set; }

        public int? RelatedMediaFileId { get; set; }

        public DateTimeOffset PlaybackTime { get; set; }

        public string FilePath { get; set; }

        [ForeignKey(nameof(RelatedMediaFileId))]
        public virtual DbMediaFile RelatedMediaFile { get; set; }
    }
}