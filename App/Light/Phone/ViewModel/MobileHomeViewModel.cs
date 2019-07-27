using GalaSoft.MvvmLight;
using Light.Core;
using Light.Model;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Phone.ViewModel
{
    class MobileHomeViewModel : ViewModelBase
    {

        private bool _isPlaying;
        private NextTrackSubset _nowPlayingTracks;

        public string NowPlayingTitle
        {
            get
            {
                return PlaybackControl.Instance.Current?.Title;
            }
        }
        public string NowPlayingArtist
        {

            get
            {
                return PlaybackControl.Instance.Current?.Artist;
            }
        }
        public ThumbnailTag BackgroundAlbumImageTag
        {
            get
            {
                var img = PlaybackControl.Instance.Current?.AlbumImageTag;
                if (img != null)
                {
                    return new ThumbnailTag
                    {
                        AlbumName = img.AlbumName,
                        ArtistName = img.ArtistName,
                        ThumbnailPath = img.ThumbnailPath,
                        Fallback = "Album,DefaultArtistLarge"
                    };
                }
                else
                {
                    return new ThumbnailTag { Fallback = "DefaultArtistLarge" };
                }
            }
        }
        public ThumbnailTag ArtistImageTag
        {
            get
            {
                var ret = PlaybackControl.Instance.Current?.ArtistImageTag;
                if (ret != null)
                    return new ThumbnailTag
                    {
                        ArtistName = ret.ArtistName,
                        Fallback = "Artist,ArtistPlaceholder"
                    };
                else return new ThumbnailTag { Fallback = "ArtistPlaceholder" };
            }
        }

        public bool IsPlaying
        {
            get { return _isPlaying; }
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    RaisePropertyChanged();
                }
            }
        }

        public NextTrackSubset NowPlayingTracks
        {
            get
            {
                return _nowPlayingTracks;
            }
            set
            {
                if (_nowPlayingTracks != value)
                {
                    _nowPlayingTracks = value;
                    RaisePropertyChanged();
                }
            }
        }

        private ObservableCollection<MusicPlaybackItem> _historyItems;

        public ObservableCollection<MusicPlaybackItem> HistoryItems
        {
            get { return _historyItems; }
            set
            {
                if (_historyItems == value)
                    return;
                _historyItems = value;
                RaisePropertyChanged();
            }
        }

        public void UpdateBindings()
        {
            //RaisePropertyChanged(null);
            RaisePropertyChanged(nameof(ArtistImageTag));
            RaisePropertyChanged(nameof(BackgroundAlbumImageTag));
            RaisePropertyChanged(nameof(NowPlayingArtist));
            RaisePropertyChanged(nameof(NowPlayingTitle));
        }
    }
}
