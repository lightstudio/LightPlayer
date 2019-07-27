using Light.Managed.Database.Entities;

namespace Light.Managed.Online
{
    public static class Banlist
    {
        public static MetadataRetrieveBanlist<DbAlbum> AlbumMetadataRetrieveBanlist { get; }
            = new MetadataRetrieveBanlist<DbAlbum>();

        public static MetadataRetrieveBanlist<DbArtist> ArtistMetadataRetrieveBanlist { get; }
            = new MetadataRetrieveBanlist<DbArtist>();
    }
}
