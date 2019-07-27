using Light.Managed.Database.Constant;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Light.Managed.Online.Groove.Model
{
    public class GrooveContentResponse
    {
        public GrooveError Error { get; set; }
        public GrooveArtistCollection Artists { get; set; }
        public GrooveAlbumCollection Albums { get; set; }
    }

    public class GrooveError
    {
        public string ErrorCode { get; set; }
        public string Description { get; set; }
        public string Message { get; set; }
    }

    public class GrooveArtist : IEntityInfo
    {
        public string Biography { get; set; }
        public string[] Genres { get; set; }
        public string[] Subgenres { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public GrooveOtherIds OtherIds { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }

        [JsonIgnore]
        public string ArtistName => Name;

        [JsonIgnore]
        public Uri Thumbnail => new Uri(ImageUrl);

        [JsonIgnore]
        public string AlbumName => null;
    }

    public class GrooveArtistCollection
    {
        public List<GrooveArtist> Items;
    }

    public class GrooveAlbumCollection
    {
        public List<GrooveAlbum> Items;
    }

    public class GrooveOtherIds
    {
        [JsonProperty("musicamg")]
        public string MusicAmg { get; set; }
    }

    public class GrooveAlbum : IEntityInfo
    {
        public DateTime ReleaseDate { get; set; }
        public string Duration { get; set; }
        public int TrackCount { get; set; }
        public bool IsExplicit { get; set; }
        public string LabelName { get; set; }
        public string[] Genres { get; set; }
        public string[] Subgenres { get; set; }
        public string AlbumType { get; set; }
        public GrooveArtistResponseEntity[] Artists { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public GrooveOtherIds OtherIds { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }

        [JsonIgnore]
        public string AlbumName => Name;

        [JsonIgnore]
        public string ArtistName => string.Join(", ", Artists.Select(x => x.Artist.Name));

        [JsonIgnore]
        public Uri Thumbnail => new Uri($"{ImageUrl}&w={DatabaseConstants.ResizedSize}&h={DatabaseConstants.ResizedSize}");
    }

    public class GrooveArtistResponseEntity
    {
        public string Role { get; set; }
        public GrooveArtistEntity Artist { get; set; }
    }

    public class GrooveArtistEntity : IEntityInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Link { get; set; }
        public string Source { get; set; }
        public string CompatibleSources { get; set; }

        [JsonIgnore]
        public string ArtistName => Name;

        [JsonIgnore]
        public Uri Thumbnail => new Uri(ImageUrl);

        [JsonIgnore]
        public string AlbumName => null;
    }

}
