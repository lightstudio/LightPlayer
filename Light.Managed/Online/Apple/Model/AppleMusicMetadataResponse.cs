using System;
using Newtonsoft.Json;
using Light.Managed.Database.Constant;

namespace Light.Managed.Online.Apple.Model
{
    public class AppleMusicSearchResult
    {
        [JsonProperty("resultCount")]
        public int ResultCount { get; set; }
        [JsonProperty("results")]
        public AppleMusicEntityResult[] Results { get; set; }
    }

    public class AppleMusicEntityResult : IEntityInfo
    {
        [JsonProperty("wrapperType")]
        public string WrapperType { get; set; }
        [JsonProperty("collectionType")]
        public string CollectionType { get; set; }
        [JsonProperty("artistId")]
        public int ArtistId { get; set; }
        [JsonProperty("collectionId")]
        public int CollectionId { get; set; }
        [JsonProperty("artistName")]
        public string ArtistName { get; set; }
        [JsonProperty("collectionName")]
        public string CollectionName { get; set; }
        [JsonProperty("collectionCensoredName")]
        public string CollectionCensoredName { get; set; }
        [JsonProperty("artistViewUrl")]
        public string ArtistViewUrl { get; set; }
        [JsonProperty("collectionViewUrl")]
        public string CollectionViewUrl { get; set; }
        [JsonProperty("artworkUrl60")]
        public string ArtworkUrl60 { get; set; }
        [JsonProperty("artworkUrl100")]
        public string ArtworkUrl100 { get; set; }
        [JsonProperty("collectionPrice")]
        public float CollectionPrice { get; set; }
        [JsonProperty("collectionExplicitness")]
        public string CollectionExplicitness { get; set; }
        [JsonProperty("trackCount")]
        public int TrackCount { get; set; }
        [JsonProperty("copyright")]
        public string Copyright { get; set; }
        [JsonProperty("country")]
        public string Country { get; set; }
        [JsonProperty("currency")]
        public string Currency { get; set; }
        [JsonProperty("releaseDate")]
        public DateTime ReleaseDate { get; set; }
        [JsonProperty("primaryGenreName")]
        public string PrimaryGenreName { get; set; }
        [JsonProperty("amgArtistI")]
        public int AmgArtistId { get; set; }
        [JsonProperty("contentAdvisoryRating")]
        public string ContentAdvisoryRating { get; set; }

        [JsonIgnore]
        public string AlbumName => CollectionName;

        [JsonIgnore]
        public Uri Thumbnail => new Uri($"{ArtworkUrl100.Substring(0, ArtworkUrl100.LastIndexOf('/'))}/{DatabaseConstants.ResizedSize}x{DatabaseConstants.ResizedSize}bb.jpg");
    }

}
