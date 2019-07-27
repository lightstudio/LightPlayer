using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;

namespace Light.Managed.Online.LastFm
{
    public class LastFmArtistEntity
    {
        [JsonProperty("artist")]
        public LastFmArtist Artist { get; set; }
    }

    public class LastFmArtist : IEntityInfo
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("mbid")]
        public string MbId { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("image")]
        public List<LastFmImageEntity> Image { get; set; }

        [JsonProperty("streamable")]
        public string Streamable { get; set; }

        [JsonProperty("ontour")]
        public string OnTour { get; set; }

        [JsonProperty("stats")]
        public LastFmStats Stats { get; set; }

        [JsonProperty("similar")]
        public LastFmSimilar Similar { get; set; }

        [JsonProperty("tags")]
        public LastFmTags Tags { get; set; }

        [JsonProperty("bio")]
        public LastFmArtistBio Bio { get; set; }

        [JsonIgnore]
        public string ArtistName => Name;

        [JsonIgnore]
        public Uri Thumbnail
        {
            get
            {
                if (Image == null || Image.Count == 0)
                {
                    return null;
                }
                Image.Sort();
                var text = Image.LastOrDefault().Text;
                if (string.IsNullOrWhiteSpace(text))
                {
                    return null;
                }
                else
                {
                    return new Uri(text);
                }
            }
        }

        public string AlbumName => null;
    }

    public class LastFmStats
    {
        [JsonProperty("listeners")]
        public string Listeners { get; set; }

        [JsonProperty("playcount")]
        public string PlayCount { get; set; }
    }

    public class LastFmSimilar
    {
        [JsonProperty("artist")]
        public LastFmArtist[] Artist { get; set; }
    }

    public class LastFmTags
    {
        [JsonProperty("tag")]
        public LastFmTag[] Tag { get; set; }
    }

    public class LastFmTag
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class LastFmArtistBio
    {
        [JsonProperty("links")]
        public LastFmLinks Links { get; set; }

        [JsonProperty("published")]
        public string Published { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("content")]
        public string Content { get; set; }
    }

    public class LastFmLinks
    {
        [JsonProperty("link")]
        public LastFmLink Link { get; set; }
    }

    public class LastFmLink
    {
        [JsonProperty("text")]
        public string Text { get; set; }

        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class LastFmImageEntity : IComparable
    {
        [JsonProperty("#text")]
        public string Text { get; set; }

        [JsonProperty("size")]
        public string Size { get; set; }

        public int CompareTo(object obj)
        {
            if (obj == null) return -1;

            if (obj is LastFmImageEntity)
            {
                var otherEntity = (LastFmImageEntity) obj;
                return GetImageSizeInt().CompareTo(otherEntity.GetImageSizeInt());
            }
            else
            {
                throw new ArgumentException("Object is not a LastFmImageEntity");
            }
        }

        private int GetImageSizeInt()
        {
            switch (Size)
            {
                case "small":
                    return 2;
                case "medium":
                    return 3;
                case "large":
                    return 4;
                case "extralarge":
                    return 5;
                case "mega":
                    return 6;
            }

            return 1;
        }
    }
}