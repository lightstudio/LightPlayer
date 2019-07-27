using GalaSoft.MvvmLight;
using Light.Core;
using Light.Managed.Tools;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LightLrcComponent;
using Light.Lyrics.External;

namespace Light.Phone.ViewModel
{
    public class MobileNowPlayingViewModel : ViewModelBase
    {
        public const string Play = "\xE768";
        public const string Pause = "\xE769";
        public const string SingleTrackLoop = "\xE8ED";
        public const string AutoRepeat = "\xE8EE";

        private string _title;
        private string _artist;
        private string _album;
        private string _playpause;
        private bool _isInFavorite = false;
        private int _thumbnailIndex;
        private TimeSpan _nowPlayingTime;
        private TimeSpan _nowPlayingTotalTime;
        private MusicPlaybackItem _currentItem;
        private string _repeat;
        private bool _lrcMissing;
        private bool _lrcSearchBusy;

        public string PlayPause
        {
            get { return _playpause; }
            set
            {
                if (value != _playpause)
                {
                    _playpause = value;
                    RaisePropertyChanged();
                }
            }
        }
        public string Repeat
        {
            get { return _repeat; }
            set
            {
                if (value != _repeat)
                {
                    _repeat = value;
                    RaisePropertyChanged();
                }
            }
        }
        public string Title
        {
            get { return _title; }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ThumbnailTag ThumbnailTag
        {
            get { return _thumbnailTag; }
            set
            {
                if (value != _thumbnailTag)
                {
                    _thumbnailTag = value;
                    RaisePropertyChanged();
                }
            }
        }
        public string Artist
        {
            get { return _artist; }
            set
            {
                if (value != _artist)
                {
                    _artist = value;
                    RaisePropertyChanged();
                }
            }
        }
        public string Album
        {
            get { return _album; }
            set
            {
                if (value != _album)
                {
                    _album = value;
                    RaisePropertyChanged();
                }
            }
        }
        public MusicPlaybackItem CurrentItem
        {
            get { return _currentItem; }
            set
            {
                if (value != _currentItem)
                {
                    _currentItem = value;
                    RaisePropertyChanged();
                }
            }
        }
        public bool IsInFavorite
        {
            get { return _isInFavorite; }
            set
            {
                _isInFavorite = value;
                RaisePropertyChanged();
            }
        }
        public ObservableCollection<MusicPlaybackItem> CurrentThumbnails => PlaybackControl.Instance.Items;
        public int SelectedThumbnailIndex
        {
            get { return _thumbnailIndex; }
            set
            {
                if (value != _thumbnailIndex)
                {
                    _thumbnailIndex = value;
                    if (PlaybackControl.Instance.Current != null)
                    {
                        var idx = PlaybackControl.Instance.Items.IndexOf(
                            PlaybackControl.Instance.Current);
                        if (value != -1 && value != idx)
                        {
                            PlaybackControl.Instance.SetIndex(value);
                        }
                    }
                    RaisePropertyChanged();
                }
            }
        }
        public double Position
        {
            get { return NowPlayingTime.TotalMilliseconds; }
            set
            {
                _nowPlayingTime = TimeSpan.FromMilliseconds(value);
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(Position));
            }
        }
        public TimeSpan NowPlayingTime
        {
            get { return _nowPlayingTime; }
            set
            {
                if (_nowPlayingTime != value)
                {
                    _nowPlayingTime = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(Position));
                }
            }
        }
        public double ItemDuration => NowPlayingTotalTime.TotalMilliseconds;
        public TimeSpan NowPlayingTotalTime
        {
            get { return _nowPlayingTotalTime; }
            set
            {
                if (value != _nowPlayingTotalTime)
                {
                    _nowPlayingTotalTime = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(ItemDuration));
                }
            }
        }

        public bool LrcMissing
        {
            get
            {
                return _lrcMissing && !NoLrcSource;
            }
            set
            {
                if (_lrcMissing == value)
                    return;
                _lrcMissing = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(NoLrcSource));
            }
        }

        public bool NoLrcSource =>
            SourceScriptManager.GetAllScripts().Length == 0 && _lrcMissing;

        public bool LrcSearchBusy
        {
            get { return _lrcSearchBusy; }
            set
            {
                if (value != _lrcSearchBusy)
                {
                    _lrcSearchBusy = value;
                    RaisePropertyChanged();
                }
            }
        }
        public ObservableCollection<ExternalLrcInfo> LrcCandidates { get; set; }
        public ThumbnailTag _thumbnailTag { get; set; }

        public void UpdateNowPlaying(MusicPlaybackItem item)
        {

            if (item == null)
            {
                return;
            }
            Title = item.Title;
            Artist = item.Artist;
            Album = item.Album;
            ThumbnailTag = item.AlbumImageTag;
            CurrentItem = item;
            NowPlayingTotalTime = item.File.Duration;
            CheckFavorite();

            NowPlayingTime = PlaybackControl.Instance.Player.Position;

            var idx = PlaybackControl.Instance.Items.IndexOf(item);
            if (idx != _thumbnailIndex)
            {
                _thumbnailIndex = idx;
                RaisePropertyChanged(nameof(SelectedThumbnailIndex));
            }
        }

        public void CheckFavorite()
        {
#if !EFCORE_MIGRATION
            if (PlaylistManager.Instance.IsInFavorite(
                    CurrentItem.File.Path,
                    CurrentItem.File.MediaCue))
            {
                IsInFavorite = true;
            }
            else
            {
                IsInFavorite = false;
            }
#endif
        }
    }
}