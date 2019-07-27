using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.Flyout;
using Light.Lyrics;
using LightLrcComponent;
using Light.Lyrics.Controls;
using Light.Lyrics.Model;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.Model;
using Light.View.Core;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Media.Playback;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Light.Shell;
using Light.Managed.Database.Native;
using Windows.Storage;
using Light.Lyrics.External;
using System.Collections.Generic;
using Light.Utilities;

namespace Light.ViewModel
{
    internal class NowPlayingViewModel : ViewModelBase
    {
        ThumbnailTag _coverImage;
        public ThumbnailTag CoverImage
        {
            get
            {
                return _coverImage;
            }
            set
            {
                if (value == _coverImage)
                    return;
                else
                {
                    _coverImage = value;
                    RaisePropertyChanged();
                }
            }
        }
        string _title;
        public string NowPlayingTitle
        {
            get
            {
                return _title;
            }
            set
            {
                if (value == _title)
                    return;
                else
                {
                    _title = value;
                    RaisePropertyChanged();
                }
            }
        }
        string _album;
        public string NowPlayingAlbum
        {
            get
            {
                return _album;
            }
            set
            {
                if (value == _album)
                    return;
                else
                {
                    _album = value;
                    RaisePropertyChanged();
                }
            }
        }
        string _artist;
        public string NowPlayingArtist
        {
            get
            {
                return _artist;
            }
            set
            {
                if (value == _artist)
                    return;
                else
                {
                    _artist = value;
                    RaisePropertyChanged();
                }
            }
        }
        private bool _busy;
        public bool LrcSearchBusy
        {
            get
            {
                return _busy;
            }
            set
            {
                if (_busy == value)
                    return;
                _busy = value;
                RaisePropertyChanged();
            }
        }
        private bool _lrcMissing = false;
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

        private NextTrackSubset _nextItems;
        public NextTrackSubset NextItems
        {
            get
            {
                return _nextItems;
            }
            set
            {
                if (_nextItems == value)
                    return;
                _nextItems = value;
                RaisePropertyChanged();
            }
        }

        private TimeSpan _nowPlayingTotalTime;
        public TimeSpan NowPlayingTotalTime
        {
            get { return _nowPlayingTotalTime; }
            set
            {
                if (_nowPlayingTotalTime == value)
                    return;
                _nowPlayingTotalTime = value;
                RaisePropertyChanged();
            }
        }

        private TimeSpan _nowPlayingTime;
        public TimeSpan NowPlayingTime
        {
            get { return _nowPlayingTime; }
            set
            {
                if (_nowPlayingTime == value)
                    return;
                _nowPlayingTime = value;
                RaisePropertyChanged();
            }
        }

        public bool NoLrcSource =>
            SourceScriptManager.GetAllScripts().Length == 0 && _lrcMissing;

        private ObservableCollection<ExternalLrcInfo> _candidates;
        public ObservableCollection<ExternalLrcInfo> LrcCandidates
        {
            get
            {
                return _candidates;
            }
            set
            {
                if (_candidates == value)
                    return;
                _candidates = value;
                RaisePropertyChanged();
            }
        }

        private bool _isInFavorite = false;
        public bool IsInFavorite
        {
            get { return _isInFavorite; }
            set
            {
                _isInFavorite = value;
                RaisePropertyChanged();
            }
        }

        public MusicPlaybackItem NowPlayingItem { get; set; }

        public GalaSoft.MvvmLight.Command.RelayCommand LrcSearchCommand { get; }
        public RelayCommand<object> PlayCommand { get; }
        public RelayCommand<object> DeleteCommand { get; }
        public RelayCommand<object> AddToPlaylistCommand { get; }
        public RelayCommand<object> LikeCommand { get; }
        public RelayCommand<object> ShareCommand { get; }
        public ObservableCollection<MusicPlaybackItem> Playlist => PlaybackControl.Instance.Items;
        private CoreDispatcher _dispatcher;
        private LyricsPresenter _presenter;

        public NowPlayingViewModel(CoreDispatcher dispatcher, LyricsPresenter presenter)
        {
            _presenter = presenter;
            _dispatcher = dispatcher;
            LrcSearchCommand = new GalaSoft.MvvmLight.Command.RelayCommand(LrcSearch);
            PlayCommand = new RelayCommand<object>(PlayItem);
            DeleteCommand = new RelayCommand<object>(DeleteItem);
            LikeCommand = new RelayCommand<object>(Like);
            AddToPlaylistCommand = new RelayCommand<object>(AddToPlaylist);
            ShareCommand = new RelayCommand<object>(Share);
        }

        private async void Share(object obj)
        {
            var path = PlaybackControl.Instance.Current?.File.Path;
            if (path == null)
                return;
            var file = await NativeMethods.GetStorageFileFromPathAsync(path) as StorageFile;
            if (file == null)
                return;
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            shareService.Title = NowPlayingTitle;
            shareService.Description = string.Empty;
            shareService.AddFile(file);
            shareService.ShowShareUI();
        }

        private async void Like(object obj)
        {
            if (IsInFavorite)
            {
                await PlaylistManager.Instance.RemoveFromFavoriteAsync(
                    PlaylistItem.FromMediaFile(NowPlayingItem.File));
            }
            else
            {
                await PlaylistManager.Instance.AddToFavoriteAsync(
                    PlaylistItem.FromMediaFile(NowPlayingItem.File));
            }
        }

        private void AddToPlaylist(object obj)
        {
            if (PlaybackControl.Instance.Current?.File != null)
            {
                PlaylistPicker.Pick(PlaylistItem.FromMediaFile(
                    PlaybackControl.Instance.Current.File));
            }
        }

        private void DeleteItem(object param)
        {
            Playlist.Remove(param as MusicPlaybackItem);
        }

        private async void PlayItem(object param)
        {
            await PlaybackControl.Instance.PlayAt(Playlist.IndexOf(param as MusicPlaybackItem));
        }

        public void RegisterEvents()
        {
            PlaylistManager.Instance.FavoriteChanged += OnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingItemChanged;
            var metadata = PlaybackControl.Instance.Current?.File;
            if (metadata != null)
            {
                NowPlayingItem = PlaybackControl.Instance.Current;
                CheckFavorite();

                NowPlayingTitle = metadata.Title;
                NowPlayingArtist = metadata.Artist;
                NowPlayingAlbum = metadata.Album;
                NowPlayingTotalTime = metadata.Duration;

                // Update cover, if available
                CoverImage = new ThumbnailTag
                {
                    Fallback = "Album,AlbumPlaceholder",
                    AlbumName = metadata.Album,
                    ArtistName = metadata.Artist,
                    ThumbnailPath = metadata.Path,
                };
            }
            if (!string.IsNullOrWhiteSpace(NowPlayingTitle))
            {
                DesktopTitleViewConfiguration.SetTitleBarText(
                    string.Format(CommonSharedStrings.NowPlayingTitle, NowPlayingTitle));
            }
            else
            {
                DesktopTitleViewConfiguration.SetTitleBarText(
                    CommonSharedStrings.NowPlayingEmptyTitle);
            }
        }

        private void CheckFavorite()
        {
#if !EFCORE_MIGRATION
            if (PlaylistManager.Instance.IsInFavorite(
                    NowPlayingItem.File.Path,
                    NowPlayingItem.File.MediaCue))
            {
                IsInFavorite = true;
            }
            else
            {
                IsInFavorite = false;
            }
#endif
        }

        private void OnFavoriteChanged(object sender, FavoriteChangedEventArgs e)
        {
            switch (e.Change)
            {
                case FavoriteChangeType.Added:
                    if (e.Item.Equals(PlaylistItem.FromMediaFile(NowPlayingItem.File)))
                    {
                        IsInFavorite = true;
                    }
                    break;
                case FavoriteChangeType.Removed:
                    if (e.Item.Equals(PlaylistItem.FromMediaFile(NowPlayingItem.File)))
                    {
                        IsInFavorite = false;
                    }
                    break;
                case FavoriteChangeType.Unknown:
                    CheckFavorite();
                    break;
            }
        }

        private async void OnNowPlayingItemChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await _dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var metadata = e.NewItem?.File;
                if (metadata == null)
                {
                    return;
                }

                NowPlayingItem = e.NewItem;
                CheckFavorite();

                NowPlayingTitle = metadata.Title;
                NowPlayingArtist = metadata.Artist;
                NowPlayingAlbum = metadata.Album;
                NowPlayingTotalTime = metadata.Duration;

                // Update cover, if available
                CoverImage = new ThumbnailTag
                {
                    Fallback = "Album,AlbumPlaceholder",
                    AlbumName = metadata.Album,
                    ArtistName = metadata.Artist,
                    ThumbnailPath = metadata.Path,
                };
                if (!string.IsNullOrWhiteSpace(NowPlayingTitle))
                {
                    DesktopTitleViewConfiguration.SetTitleBarText(
                        string.Format(CommonSharedStrings.NowPlayingTitle, NowPlayingTitle));
                }
                else
                {
                    DesktopTitleViewConfiguration.SetTitleBarText(CommonSharedStrings.NowPlayingEmptyTitle);
                }
                //Call LrcAutoSearch on playlist item changed
                await LrcAutoSearch();
            });
        }

        public void UnregsterEvents()
        {
            PlaylistManager.Instance.FavoriteChanged -= OnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingItemChanged;
        }
        public async Task<bool> LrcAutoSearch()
        {
            LrcMissing = false;
            LrcSearchBusy = true;
            _presenter.Lyrics = null;
            var _ttitle = _title;
            var _tartist = _artist;
            LrcCandidates = new ObservableCollection<ExternalLrcInfo>();
            ParsedLrc lrc = null;
            try
            {
                lrc = await LyricsAgent.FetchLyricsAsync(
                    _title, _artist, _candidates,
                    LyricsAgent.BuildLyricsFilename(_title, _artist));
            }
            catch
            {

            }
            if (_ttitle == _title && _tartist == _artist)
            {
                _presenter.Lyrics = lrc;
                LrcSearchBusy = false;
                LrcMissing = lrc == null || lrc.Sentences.Count == 0;
                return true;
            }
            return false;
        }
        private async void LrcSearch()
        {
            try
            {
                var modifyFlyout = new LyricManualSelectionFlyout();
                modifyFlyout.LrcSelected += ModifyFlyoutOnItemSaved;
                await modifyFlyout.ShowAsync(_title, _artist, _candidates);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        private void ModifyFlyoutOnItemSaved(object sender, LrcSelectedEventArgs e)
        {
            ((LyricManualSelectionFlyout)sender).LrcSelected -= ModifyFlyoutOnItemSaved;
            _presenter.Lyrics = e.Lrc;
            LrcMissing = e.Lrc == null;
        }
    }
}
