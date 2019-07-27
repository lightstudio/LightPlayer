using GalaSoft.MvvmLight;
using Light.Common;
using Light.Converter;
using Light.Managed.Database.Entities;
using System;

namespace Light.Model
{
    /// <summary>
    /// Common-purpose model class for views.
    /// </summary>
    public class CommonViewItemModel : ViewModelBase
    {
        /// <summary>
        /// Entity database primary key ID for reference.
        /// </summary>
        public int InternalDbEntityId { get; private set; }

        /// <summary>
        /// Entity title.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Entity subtitle or content.
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Path of the entity's image.
        /// </summary>
        public string ImagePath { get; set; }

        /// <summary>
        /// Type of the entity.
        /// </summary>
        public CommonItemType Type { get; set; }

        /// <summary>
        /// Genre of the entity.
        /// </summary>
        public string Genre { get; set; }

        /// <summary>
        /// Release date of the entity.
        /// </summary>
        public string ReleaseDate { get; set; }

        /// <summary>
        /// Hash value of the image.
        /// </summary>
        public string ImageHash { get; set; }

        /// <summary>
        /// Extended file path.
        /// </summary>
        public string ExtendedFilePath { get; set; }

        /// <summary>
        /// Extended artist name.
        /// </summary>
        public string ExtendedArtistName { get; set; }

        /// <summary>
        /// Time of the entity being added.
        /// </summary>
        public DateTimeOffset DatabaseItemAddedDate { get; set; }

        /// <summary>
        /// Instance of <see cref="DbMediaFile"/> for internal reference.
        /// </summary>
        public DbMediaFile File { get; set; }

        /// <summary>
        /// Load album entity.
        /// </summary>
        /// <param name="album">Instance of <see cref="DbAlbum"/>.</param>
        private void LoadAlbum(DbAlbum album)
        {
            if (album == null) throw new ArgumentNullException(nameof(album));
            Type = CommonItemType.Album;
            InternalDbEntityId = album.Id;
            Title = album.Title ?? "Album";
            var name = album.Artist ?? "Artist";
            ExtendedArtistName = name;
            var songsCount = album.FileCount;

            if (songsCount > 0)
            {
                Content = string.Format(CommonSharedStrings.AlbumSubtitleFormat, songsCount, name);
            }
            else
            {
                Content = string.Format(CommonSharedStrings.AlbumSubtitleFallbackFormat, name);
            }

            Genre = album.Genre ?? "Genre";
            ReleaseDate = album.Date ?? "";
            ImagePath = ImageHash = album.CoverPath;
            ExtendedFilePath = album.FirstFileInAlbum;
            DatabaseItemAddedDate = album.DatabaseItemAddedDate;
        }

        /// <summary>
        /// Initializes new instance of <see cref="CommonViewItemModel"/> from album entity.
        /// </summary>
        /// <param name="album">Instance of <see cref="DbAlbum"/>.</param>
        public CommonViewItemModel(DbAlbum album)
        {
            LoadAlbum(album);
        }

        /// <summary>
        /// Initializes new instance of <see cref="CommonViewItemModel"/> from artist entity.
        /// </summary>
        /// <param name="artist">Instance of <see cref="DbArtist"/>.</param>
        public CommonViewItemModel(DbArtist artist)
        {
            if (artist == null) throw new ArgumentNullException(nameof(artist));
            Type = CommonItemType.Artist;

            Title = artist.Name ?? "Artist";
            InternalDbEntityId = artist.Id;
            ExtendedArtistName = artist.Name;
            
            var songsCount = artist.FileCount;
            var albumCount = artist.AlbumCount;

            if (songsCount > 0 && albumCount > 0)
            {
                Content = string.Format(CommonSharedStrings.ArtistSubtitleFormat, albumCount, songsCount);
            }
            else if (songsCount > 0)
            {
                Content = string.Format(CommonSharedStrings.ArtistSubtitleFallbackFormat2, songsCount);
            }
            else
            {
                Content = CommonSharedStrings.ArtistSubtitleFallbackFormat;
            }

            Genre = ReleaseDate = string.Empty;
            ImagePath = ImageHash = artist.ImagePath;
            ExtendedFilePath = string.Empty;
            DatabaseItemAddedDate = artist.DatabaseItemAddedDate;
        }

        /// <summary>
        /// Initializes new instance of <see cref="CommonViewItemModel"/> from file entity.
        /// </summary>
        /// <param name="file">Instance of <see cref="DbMediaFile"/>.</param>
        /// <param name="disableArtistInfo">Whether disables artist information.</param>
        public CommonViewItemModel(DbMediaFile file, bool disableArtistInfo = false)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            Title = file.Title ?? CommonSharedStrings.DefaultFileName;

            if (file.AlbumArtist == null)
                file.AlbumArtist = CommonSharedStrings.UnknownArtistTitle;

            if (file.Artist == null)
                file.Artist = CommonSharedStrings.UnknownArtistTitle;

            if (file.Album == null)
                file.Album = CommonSharedStrings.UnknownAlbumTitle;

            if (!disableArtistInfo)
            {
                Content = string.Format(CommonSharedStrings.SongSubtitleFormat,
                    file.Album ?? CommonSharedStrings.DefaultAlbumName, file.Artist,
                    MiliSecToNormalTimeConverter.GetTimeStringFromTimeSpanOrDouble(file.Duration));
            }
            else
            {
                Content = MiliSecToNormalTimeConverter.GetTimeStringFromTimeSpanOrDouble(file.Duration);
            }

            InternalDbEntityId = file.Id;
            Type = CommonItemType.Song;
            ReleaseDate = file.Date;
            Genre = file.Genre;
            ExtendedFilePath = file.Path;
            ExtendedArtistName = file.Artist;
            File = file;
            DatabaseItemAddedDate = file.DatabaseItemAddedDate;
        }

        /// <summary>
        /// Helper that verifies and creates new instance of <see cref="CommonViewItemModel"/> from album entity.
        /// </summary>
        /// <param name="album">Instance of <see cref="DbAlbum"/>.</param>
        /// <returns>Instance of <see cref="CommonViewItemModel"/>. Will return null if given album has null or empty title.</returns>
        public static CommonViewItemModel CreateFromDbAlbumAndCheck(DbAlbum album)
        {
            if (string.IsNullOrEmpty(album?.Title))
            {
                return null;
            }
            else
            {
                return new CommonViewItemModel(album);
            }
        }

        /// <summary>
        /// Helper that verifies and creates new instance of <see cref="CommonViewItemModel"/> from artist entity.
        /// </summary>
        /// <param name="artist">Instance of <see cref="DbArtist"/>.</param>
        /// <returns>Instance of <see cref="CommonViewItemModel"/>. Will return null if given artist has null or empty name.</returns>
        public static CommonViewItemModel CreateFromDbArtistAndCheck(DbArtist artist)
        {
            if (string.IsNullOrEmpty(artist?.Name))
            {
                return null;
            }
            else
            {
                return new CommonViewItemModel(artist);
            }
        }

    }

    /// <summary>
    /// Types of <see cref="CommonViewItemModel"/>.
    /// </summary>
    public enum CommonItemType
    {
        Album = 0,
        Artist = 1,
        Song = 2,
        Genre = 3,
        Playlist = 4,
        Other = 5,
        Search = 6,
        NowPlaying = 7,
        Settings = 8,
        LyricsItem = 9,
        AlbumDetails = 10,
        ArtistDetails = 11,
        PlaylistDetails = 12,
        FileShare = 13
    }
}
