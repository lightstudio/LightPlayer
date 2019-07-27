using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;

namespace Light.Managed.Tools
{
    class MusicPropertiesMediaInfo : IMediaInfo
    {
        public string Album { get; set; }
        public string AlbumArtist { get; set; }
        public string Artist { get; set; }
        public string Comments { get; set; } = string.Empty;
        public string Composer { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public string Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public string DiscNumber { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public string Genre { get; set; }
        public string Grouping { get; set; } = string.Empty;
        public string Performer { get; set; } = string.Empty;
        public string Title { get; set; }
        public string TotalDiscs { get; set; } = string.Empty;
        public string TotalTracks { get; set; } = string.Empty;
        public string TrackNumber { get; set; } = string.Empty;
        public IReadOnlyDictionary<string, string> AllProperties { get; set; } = null;
        public static MusicPropertiesMediaInfo Create(MusicProperties properties)
        {
            var ret = new MusicPropertiesMediaInfo
            {
                Album = properties.Album,
                AlbumArtist = properties.AlbumArtist,
                Artist = properties.Artist,
                Date = properties.Year == 0 ? "" : properties.Year.ToString(),
                Title = properties.Title,
                TrackNumber = properties.TrackNumber.ToString(),
                Duration = properties.Duration
            };
            if (properties.Genre.Count != 0)
                ret.Genre = properties.Genre[0];
            else ret.Genre = string.Empty;
            return ret;
        }
    }
}
