using GalaSoft.MvvmLight;
using Light.Core;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.ViewModel.Library
{
    public class HomeViewModel : ViewModelBase
    {
        private MusicPlaybackItem _item;

        public HomeViewModel()
        {
            _item = PlaybackControl.Instance.Current;
        }

        public string NowPlayingTitle => _item?.Title;
        public string NowPlayingArtist => _item?.Artist;
        public string NowPlayingAlbum => _item?.Album;

        public ThumbnailTag AlbumImageTag
        {
            get
            {
                return _item?.AlbumImageTag ?? new ThumbnailTag { Fallback = "AlbumPlaceholder" };
            }
        }

        public ThumbnailTag ArtistImageTag
        {
            get
            {
                var img = _item?.ArtistImageTag;
                if (img != null)
                    return new ThumbnailTag
                    {
                        ArtistName = img.ArtistName,
                        Fallback = "Artist,DefaultArtistLarge"
                    };
                else return new ThumbnailTag { Fallback = "DefaultArtistLarge" };
            }
        }

        private Utilities.NextTrackSubset _nextTracks;
        public Utilities.NextTrackSubset NextTracks
        {
            get { return _nextTracks; }
            set
            {
                if (_nextTracks == value)
                    return;
                _nextTracks = value;
                RaisePropertyChanged();
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

        private void UpdateAllProperties()
        {
            RaisePropertyChanged(nameof(ArtistImageTag));
            RaisePropertyChanged(nameof(AlbumImageTag));
            RaisePropertyChanged(nameof(NowPlayingAlbum));
            RaisePropertyChanged(nameof(NowPlayingArtist));
            RaisePropertyChanged(nameof(NowPlayingTitle));
        }

        public void UpdateBindings()
        {
            _item = PlaybackControl.Instance.Current;
            UpdateAllProperties();
        }

        public void UpdateBindings(MusicPlaybackItem item)
        {
            _item = item;
            UpdateAllProperties();
        }
    }
}
