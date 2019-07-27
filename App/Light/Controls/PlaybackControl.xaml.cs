using ColorThiefDotNet;
using GalaSoft.MvvmLight.Messaging;
using Light.Annotations;
using Light.Common;
using Light.Core;
using Light.Managed.Tools;
using Light.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;

namespace Light.Controls
{
    public sealed partial class PlaybackControl : UserControl, INotifyPropertyChanged
    {
        public bool IsInNowPlaying
        {
            get { return (bool)GetValue(IsInNowPlayingProperty); }
            set
            {
                var changed = (value != IsInNowPlaying);
                SetValue(IsInNowPlayingProperty, value);
                OnPropertyChanged(nameof(ControlHeight));
                if (changed && !value)
                    UpdateBackgroundColor();
            }
        }

        public static readonly DependencyProperty IsInNowPlayingProperty =
            DependencyProperty.Register("IsInNowPlaying", typeof(bool), typeof(PlaybackControl), new PropertyMetadata(false));

        private bool IsGlobal
        {
            get
            {
                return !IsInNowPlaying;
            }
        }

        public static class MediaControlGlyphs
        {
            public static Dictionary<PlaybackMode, string> GlyphMap =
                new Dictionary<PlaybackMode, string>
                {
                    {PlaybackMode.ListLoop, AutoRepeat },
                    {PlaybackMode.Random, Shuffle },
                    {PlaybackMode.Sequential, Sequential },
                    {PlaybackMode.SingleTrackLoop, Loop }
                };
            public const string Sequential = "\xEDE1";
            public const string Loop = "\xE8ED";
            public const string AutoRepeat = "\xE8EE";
            public const string Shuffle = "\xE8B1";

            public const string VolumeDisabled = "\xE198";
            public const string VolumeLevel0 = "\xE992";
            public const string VolumeLevel1 = "\xE993";
            public const string VolumeLevel2 = "\xE994";
            public const string VolumeLevel3 = "\xE995";

            public const string Play = "\xE768";
            public const string Pause = "\xE769";
        }

        private bool _loaded = false;
        private string _playPauseButtonSymbol;
        private DispatcherTimer _timer;
        private double _itemDuration;
        private double _position;
        private bool _isInFavorite = false;
        public MusicPlaybackItem NowPlayingItem { get; set; }

        #region Bindings
        public bool ShowAllControls
        {
            get { return _showAllControls; }
            set
            {
                if (value == _showAllControls) return;
                if (value)
                {
                    NextButton.Margin = new Thickness(6.5, 0, 6.5, 0);
                }
                else
                {
                    NextButton.Margin = new Thickness(6.5, 0, 30, 0);
                }
                _showAllControls = value;
                OnPropertyChanged();
            }
        }

        public string PlayPauseButtonSymbol
        {
            get { return _playPauseButtonSymbol; }
            set
            {
                if (value == _playPauseButtonSymbol) return;
                _playPauseButtonSymbol = value;
                OnPropertyChanged();
            }
        }
        public double ItemDuration
        {
            get { return _itemDuration; }
            set
            {
                if (value.Equals(_itemDuration)) return;
                _itemDuration = value;
                OnPropertyChanged();
            }
        }
        public double Position
        {
            get { return _position; }
            set
            {
                if (value.Equals(_position)) return;
                _position = value;
                OnPropertyChanged();
            }
        }
        public bool IsControlAvailable
        {
            get { return _isControlAvailable; }
            set
            {
                if (value == _isControlAvailable) return;
                _isControlAvailable = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsGlobalControlAvailable));
                OnPropertyChanged(nameof(ControlHeight));
            }
        }

        public double ControlHeight
        {
            get
            {
                return IsControlAvailable ?
                    IsInNowPlaying ? 60 : 80
                    : 0;
            }
        }

        public bool IsGlobalControlAvailable
        {
            get
            {
                return _isControlAvailable;
            }
        }

        public double Remaining
        {
            get { return _remaining; }
            set
            {
                if (value.Equals(_remaining)) return;
                _remaining = value;
                OnPropertyChanged();
            }
        }
        public ICommand ModeButtonCommand { get; set; }
        public string ModeGlyph
        {
            get { return _modeGlyph; }
            set
            {
                if (value == _modeGlyph) return;
                _modeGlyph = value;
                OnPropertyChanged();
            }
        }
        public double Volume
        {
            get { return _volume; }
            set
            {
                if (value.Equals(_volume)) return;
                _volume = value;
                NowPlayingStateManager.Volume = value;

                OnPropertyChanged();
                OnPropertyChanged(nameof(VolumeButtonGlyph));
                Core.PlaybackControl.Instance.SetVolume(_volume / 100);
            }
        }
        public string VolumeButtonGlyph
        {
            get
            {
                if (Volume <= 0.0) return MediaControlGlyphs.VolumeDisabled;
                if (0.0 < Volume && Volume <= 33.0) return MediaControlGlyphs.VolumeLevel1;
                if (33.0 < Volume && Volume <= 66.0) return MediaControlGlyphs.VolumeLevel2;
                return MediaControlGlyphs.VolumeLevel3;
            }
        }
        public ThumbnailTag CoverImageTag
        {
            get { return _coverImageTag; }
            set
            {
                _coverImageTag = value;
                OnPropertyChanged();
            }
        }

        public bool IsInFavorite
        {
            get { return _isInFavorite; }
            set
            {
                _isInFavorite = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region NowPlaying
        private string _title;
        private string _artist;
        private string _album;
        private bool _isControlAvailable;
        private double _remaining;
        private string _modeGlyph;
        private double _volume;
        private ThumbnailTag _coverImageTag;
        private bool _showAllControls = true;

        public string Title
        {
            get { return _title; }
            set
            {
                if (value == _title) return;
                _title = value;
                OnPropertyChanged();
            }
        }
        public string Artist
        {
            get { return _artist; }
            set
            {
                if (value == _artist) return;
                _artist = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Dot));
            }
        }
        public string Album
        {
            get { return _album; }
            set
            {
                if (value == _album) return;
                _album = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Dot));
            }
        }
        public string Dot
        {
            get
            {
                return (
                    string.IsNullOrWhiteSpace(Artist) ||
                    string.IsNullOrWhiteSpace(Album))
                    ? "" : "·";
            }
        }
        #endregion

        public PlaybackControl()
        {
            this.InitializeComponent();

            ExtendedSlider.ValueChangeStarting += ExtendedSliderOnValueChangeStarting;
            ExtendedSlider.ValueChangeCompleted += ExtendedSliderOnValueChangeCompleted;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _timer.Tick += TimerOnTick;

            ModeButtonCommand = new ModeButtonCommandClass(this);
            PlaylistManager.Instance.FavoriteChanged += OnFavoriteChanged;
            LayoutRoot.Background = (Brush)Resources["SystemControlHighlightAccentBrush"];
        }

        #region Event Handlers
        private void ExtendedSliderOnValueChangeCompleted(object sender, SliderValueChangeCompletedEventArgs args)
        {
            _timer.Start();
        }
        private void ExtendedSliderOnValueChangeStarting(object sender, EventArgs eventArgs)
        {
            _timer.Stop();
        }
        private async void OnPlaybackStateChanged(object sender, object args)
        {
            var state = Core.PlaybackControl.Instance.Player.CurrentState;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                switch (state)
                {
                    case MediaElementState.Buffering:
                        _timer.Stop();
                        PlayPauseButtonSymbol = MediaControlGlyphs.Pause;
                        PlayPauseToolTip.Content = CommonSharedStrings.ToolTipPause;
                        break;
                    case MediaElementState.Playing:
                        _timer.Start();
                        PlayPauseButtonSymbol = MediaControlGlyphs.Pause;
                        PlayPauseToolTip.Content = CommonSharedStrings.ToolTipPause;
                        break;
                    case MediaElementState.Opening:
                    case MediaElementState.Paused:
                    case MediaElementState.Closed:
                    case MediaElementState.Stopped:
                        _timer.Stop();
                        PlayPauseButtonSymbol = MediaControlGlyphs.Play;
                        PlayPauseToolTip.Content = CommonSharedStrings.ToolTipPlay;
                        break;
                }
            });
        }

        private void TimerOnTick(object sender, object o) =>
            Remaining = ItemDuration - (Position = Core.PlaybackControl.Instance.Player.Position.TotalMilliseconds);
        #endregion


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
            if (NowPlayingItem != null)
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
        }

        private async void OnNowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (!IsControlAvailable) IsControlAvailable = true;
                if (e.NewItem == null) return;
                // Update item
                NowPlayingItem = e.NewItem;
                CheckFavorite();
                // Update duration
                ItemDuration = NowPlayingItem.File.Duration.TotalMilliseconds;
                Position = 0;
                Remaining = ItemDuration;

                var metadata = Core.PlaybackControl.Instance.Current?.File;

                if (metadata != null)
                {
                    Title = metadata.Title;
                    Artist = metadata.Artist;
                    Album = metadata.Album;
                    // Update cover, if available
                    CoverImageTag = new ThumbnailTag
                    {
                        Fallback = "Album,AlbumPlaceholder",
                        AlbumName = metadata.Album,
                        ArtistName = metadata.Artist,
                        ThumbnailPath = metadata.Path,
                    };
                }
            });
        }



        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void InitializePlaybackPrefs()
        {
            IsControlAvailable = false;
            Title = Artist = Album = string.Empty;
            PlayPauseButtonSymbol = MediaControlGlyphs.Play;
            Core.PlaybackControl.Instance.Mode = (PlaybackMode)NowPlayingStateManager.PlaybackMode;
            ModeGlyph = MediaControlGlyphs.GlyphMap[Core.PlaybackControl.Instance.Mode];
            Volume = NowPlayingStateManager.Volume;
        }
        public void PlaybackModeMoveNext()
        {
            Core.PlaybackControl.Instance.Mode = (Core.PlaybackMode)((
                (int)Core.PlaybackControl.Instance.Mode + 1) %
                MediaControlGlyphs.GlyphMap.Count);
            ModeGlyph = MediaControlGlyphs.GlyphMap[Core.PlaybackControl.Instance.Mode];
            NowPlayingStateManager.PlaybackMode = (int)Core.PlaybackControl.Instance.Mode;
        }

        public class ModeButtonCommandClass : ICommand
        {
            private readonly PlaybackControl _parent;
            public ModeButtonCommandClass(PlaybackControl parent)
            {
                _parent = parent;
            }

            public bool CanExecute(object parameter) => true;

            public void Execute(object parameter)
            {
                _parent.PlaybackModeMoveNext();
            }

#pragma warning disable CS0067
            public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
        }

        private void OnMetadataPanelTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            Messenger.Default.Send(new GenericMessage<bool>(true), "ShowNowPlayingView");
        }

        private void OnPlaybackControlLoaded(object sender, RoutedEventArgs e)
        {
            InitializePlaybackPrefs();

            Core.PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            Core.PlaybackControl.Instance.Player.CurrentStateChanged += OnPlaybackStateChanged;
        }

        private async void OnFavoriteButtonClicked(object sender, RoutedEventArgs e)
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

        /// <summary>
        /// Update background color using album cover as a baseline reference.
        /// </summary>
        /// <remarks>
        /// This method contains lots of workarounds, DO NOT TOUCH THIS UNLESS YOU ABSOLUTELY KNOW WHAT YOU ARE DOING.
        /// </remarks>
        private async void UpdateBackgroundColor()
        {
            if (_loaded)
                await Task.Delay(100);
            else
            {
                await Task.Delay(1000);
                _loaded = true;
            }
            if (IsInNowPlaying)
                return;

            var target = new RenderTargetBitmap();
            try
            {
                await target.RenderAsync(CoverImage);
            }
            catch
            {
                // workaround for RenderAsync throwing ArgumentOutOfRangeException 
                // when target control is not visible.
                return;
            }

            var isColorRetrievalApplicable = true;
            SolidColorBrush currentBrush = (SolidColorBrush)LayoutRoot.Background;
            SolidColorBrush candidateBrush = null;

            var pixels = await target.GetPixelsAsync();
            if (pixels.Length == 0)
            {
                isColorRetrievalApplicable = false;
            }
            else
            {
                var t = new ColorThief();
                var q = t.GetColor(pixels.ToArray());
                if (q == null)
                {
                    isColorRetrievalApplicable = false;
                }

                // If a candidate color can be retrieved, it will be retrieved using album reference.
                if (isColorRetrievalApplicable)
                {
                    byte a = q.Color.A,
                    r = q.Color.R,
                    g = q.Color.G,
                    b = q.Color.B;
                    if (!q.IsDark)
                    {
                        r /= 2;
                        g /= 2;
                        b /= 2;
                    }
                    var result = Windows.UI.Color.FromArgb(a, r, g, b);
                    candidateBrush = new SolidColorBrush(result);
                }
            }

            // Fallback color for no reference items
            if (!isColorRetrievalApplicable)
            {
                candidateBrush = (SolidColorBrush)Resources["SystemControlHighlightAccentBrush"];
            }

            // Animation first. Real value will be set later.
            var colorTransitionStoryBoard = (Storyboard)Resources["TransitionColorStoryboard"];
            ((ColorAnimation)colorTransitionStoryBoard.Children[0]).To = candidateBrush.Color;

            // Let it fire!
            colorTransitionStoryBoard.Begin();
        }

        private void CoverImage_ImageChanged(object sender, EventArgs e)
        {
            UpdateBackgroundColor();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width > 750)
            {
                if (!_showAllControls)
                {
                    ShowAllControls = true;
                }
            }
            else
            {
                if (_showAllControls)
                {
                    ShowAllControls = false;
                }
            }
        }
    }
}
