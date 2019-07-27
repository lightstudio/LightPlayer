using Light.Managed.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Globalization.DateTimeFormatting;

namespace Light.Model
{
    public class RecentlyAddedAlbumModel
    {
        public RecentlyAddedAlbumModel(DbAlbum album)
        {
            Album = album;
        }
        public string Title => Album.Title;
        public string Artist => Album.Artist;
        public ThumbnailTag Thumbnail => new ThumbnailTag
        {
            AlbumName = Album.Title,
            ArtistName = Album.Artist,
            Fallback = "Album,AlbumPlaceholder",
            ThumbnailPath = Album.FirstFileInAlbum
        };
        public string AddedDate
        {
            get
            {
                DateTimeFormatter dtf = new DateTimeFormatter("shortdate");
                return dtf.Format(Album.DatabaseItemAddedDate);
            }
        }
        public DbAlbum Album { get; private set; }
    }
}
