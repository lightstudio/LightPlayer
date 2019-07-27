using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light;

namespace Light.CueIndex
{
    public class CueMediaInfo : IMediaInfo
    {
        public string Album { get; set; } = string.Empty;//cue.Title, "album"
        public string AlbumArtist { get; set; } = string.Empty;//cue.Performer, "album_artist"
        public string Artist { get; set; } = string.Empty;//track.Performer or cue.Performer, "artist"
        public string Comments { get; set; } = string.Empty;//track.Comments, "comment"
        public string Composer { get; set; } = string.Empty;//track.Songwriter or cue.Songwriter, "composer"
        public string Copyright { get; set; } = string.Empty;//null
        public string Date { get; set; } = string.Empty;//REM DATE, "date"
        public string Description { get; set; } = string.Empty;//null
        public string DiscNumber { get; set; } = string.Empty;//1, "disc"
        public TimeSpan Duration { get; set; }//duration, not a key
        public string Genre { get; set; } = string.Empty;//REM GENRE, "genre"
        public string Grouping { get; set; } = string.Empty;//null
        public string Performer { get; set; } = string.Empty;//track.Performer or cue.Performer, "performer"
        public string Title { get; set; } = string.Empty;//track.Title, "title"
        public string TotalDiscs { get; set; } = string.Empty;//1, "disc"
        public string TotalTracks { get; set; } = string.Empty;//cue.Tracks.Length, "track"
        public string TrackNumber { get; set; } = string.Empty;//track.TrackNumber, "track"
        public IReadOnlyDictionary<string, string> AllProperties { get; set; }//All above and all REMs
    }
}
