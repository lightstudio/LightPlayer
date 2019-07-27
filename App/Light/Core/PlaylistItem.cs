using Light.Common;
using Light.Converter;
using Light;
using Light.Managed.Database.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light.CueIndex;
using Windows.Foundation.Metadata;

namespace Light.Core
{
    public class Playlist
    {
        static bool CanExportWpl => ApiInformation.IsApiContractPresent("Windows.Media.Playlists.PlaylistsContract", 1);
        public ObservableCollection<PlaylistItem> Items { get; set; } = new ObservableCollection<PlaylistItem>();
        public string Title { get; set; }
        public string ImagePath { get; set; }
        [JsonIgnore]
        public string Subtitle
        {
            get
            {
                switch (Items.Count)
                {
                    case 0:
                        return CommonSharedStrings.PlaylistNoItemSubtitle;
                    case 1:
                        return CommonSharedStrings.PlaylistSingleItemSubtitle;
                    default:
                        return string.Format(
                            CommonSharedStrings.PlaylistSubtitle,
                            Items.Count);
                }
            }
        }

        [JsonIgnore]
        public bool CanExportPlaylist => CanExportWpl;

        [JsonIgnore]
        public bool IsFavorite => this == PlaylistManager.Instance.FavoriteList;

        [JsonIgnore]
        public string DeleteText
        {
            get
            {
                return IsFavorite ?
                    CommonSharedStrings.ClearString :
                    CommonSharedStrings.DeleteString;
            }
        }

        public Playlist Duplicate(string name)
        {
            var newList = new Playlist { Title = name };
            foreach (var item in Items)
            {
                newList.Items.Add(item);
            }
            return newList;
        }
    }
    public class PlaylistItem
    {
        public string Title { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string Path { get; set; }
        public TimeSpan Duration { get; set; }
        public ManagedAudioIndexCue Cue { get; set; }

        [JsonIgnore]
        public string Content =>
            string.Format(CommonSharedStrings.SongSubtitleFormat,
                string.IsNullOrWhiteSpace(Album) ? CommonSharedStrings.DefaultAlbumName : Album,
                Artist,
                MiliSecToNormalTimeConverter.GetTimeStringFromTimeSpanOrDouble(Duration));

        public static PlaylistItem FromMediaFile(DbMediaFile file)
        {
            var item = new PlaylistItem
            {
                Title = file.Title,
                Artist = file.Artist,
                Album = file.Album,
                Path = file.Path,
                Duration = file.Duration,
#if !EFCORE_MIGRATION
                Cue = file.MediaCue
#endif
            };
#if !EFCORE_MIGRATION
            if (item.Cue != null)
                item.Cue.TrackInfo = null;
#endif
            return item;
        }
        public DbMediaFile ToMediaFile()
        {
            return new DbMediaFile
            {
                Title = Title,
                Artist = Artist,
                Album = Album,
                AlbumArtist = "",
                Path = Path,
                Duration = Duration,
                StartTime = (int?)Cue?.StartTime.TotalMilliseconds,
                Id = -65535,
                IsExternal = true
            };
        }
        public bool Equals(PlaylistItem item)
        {
            if (item == null)
                return false;
            if (item.Path != Path)
                return false;
            if ((item.Cue == null) !=
                (Cue == null))
            {
                return false;
            }
            if (item.Cue != null &&
                (item.Cue.StartTime != Cue.StartTime ||
                item.Cue.Duration != Cue.Duration))
            {
                return false;
            }
            return true;
        }
    }
}
