using System;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Light.Common;
using Windows.UI.Xaml.Media;

namespace Light.Controls
{
    public sealed partial class MediaPlaybackItemIndicator : UserControl
    {
        private Core.MusicPlaybackItem _backendField;
        private bool _isCurrent = false;
        public Core.MusicPlaybackItem BackendField
        {
            get { return _backendField; }
            set
            {
                _backendField = value;

                // Load initial state
                CheckCurrentItemStatus(Core.PlaybackControl.Instance.Current);
            }
        }

        public MediaPlaybackItemIndicator()
        {
            this.InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Core.PlaybackControl.Instance.Player.CurrentStateChanged -= OnPlayerCurrentStateChanged;
            Core.PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
        }

        private void OnLoaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Core.PlaybackControl.Instance.Player.CurrentStateChanged += OnPlayerCurrentStateChanged;
            Core.PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
        }

        private async void OnPlayerCurrentStateChanged(object sender, object args)
        {
            if (_isCurrent)
            {
                await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
                {
                    PlaybackStatusIcon.Text =
                        Core.PlaybackControl.Instance.Player.CurrentState == MediaElementState.Playing ?
                            CommonSharedStrings.PlayingTextGlyph : CommonSharedStrings.PausedTextGlyph;
                });
            }
        }

        private async void OnNowPlayingChanged(object sender, Core.NowPlayingChangedEventArgs e)
        {
            if (BackendField == null || e.NewItem == null) return;
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                CheckCurrentItemStatus(e.NewItem);
            });
        }

        private void CheckCurrentItemStatus(Core.MusicPlaybackItem glbItem)
        {
            if (glbItem == null) return;
            if (glbItem == BackendField)
            {
                _isCurrent = true;
                PlaybackStatusIcon.Text =
                    Core.PlaybackControl.Instance.Player.CurrentState == MediaElementState.Playing ?
                        CommonSharedStrings.PlayingTextGlyph : CommonSharedStrings.PausedTextGlyph;
            }
            else
            {
                _isCurrent = false;
                PlaybackStatusIcon.Text = "";
            }
        }
    }
}
