using Light.Common;
using Light.Managed.Database.Entities;

namespace Light.Model
{
    public class SearchResultModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Artist { get; set; }
        public object Entity { get; set; }
        public CommonItemType ItemType { get; set; }
        public bool HasThumbnail { get; set; }
        public ThumbnailTag Thumbnail { get; set; }
        public SearchResultModel(DbMediaFile file)
        {
            Entity = file;
            Title = file.Title;
            Subtitle = CommonSharedStrings.Music;
            ItemType = CommonItemType.Song;
            HasThumbnail = false;
            Artist = file.Artist;
        }

        public SearchResultModel(DbAlbum album)
        {
            Entity = album;
            Title = album.Title;
            Subtitle = CommonSharedStrings.AlbumText;
            ItemType = CommonItemType.Album;
            HasThumbnail = true;
            Artist = album.Artist;
            Thumbnail = new ThumbnailTag
            {
                Fallback = "Album,AlbumPlaceholder",
                ArtistName = album.Artist,
                AlbumName = album.Title,
                ThumbnailPath = album.FirstFileInAlbum,
            };
        }

        public SearchResultModel(DbArtist artist)
        {
            Entity = artist;
            Title = artist.Name;
            Artist = artist.Name;
            Subtitle = CommonSharedStrings.ArtistText;
            ItemType = CommonItemType.Artist;
            HasThumbnail = true;
            Thumbnail = new ThumbnailTag
            {
                ArtistName = artist.Name,
                Fallback = "Artist,ArtistPlaceholder"
            };
        }
        public override string ToString()
        {
            return Title;
        }
    }
}
