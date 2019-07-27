using ColorThiefDotNet;
using Light.Common;
using Light.Core;
using Light.Managed.Tools;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Composition.Interactions;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Composition;
using Windows.UI.Input;
using GalaSoft.MvvmLight.Messaging;
using Windows.ApplicationModel;
using Windows.UI.Popups;
using Light.Utilities;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileFrameView : Page
    {
        public MobileFrameView()
        {
            this.InitializeComponent();
            InitializeNowPlaying();
        }

        private void UpdateVisibleBoundsPadding()
        {
            if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                var bottomVisibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
                VisibleBoundPadding.Height = ActualHeight - bottomVisibleBounds.Bottom;
                NowPlayingPage.Height = bottomVisibleBounds.Bottom;
            }
            else
            {
                // Development purpose only.
                NowPlayingPage.Height = ActualHeight;
            }
        }

        private void OnVisibleBoundsChanged(ApplicationView sender, object args)
        {
            UpdateVisibleBoundsPadding();
        }

        private async void OnPlaybackStateChanged(object sender, RoutedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                switch (PlaybackControl.Instance.Player.CurrentState)
                {
                    case MediaElementState.Opening:
                    case MediaElementState.Buffering:
                    case MediaElementState.Playing:
                        PlayPauseGlyph.Glyph = Controls.PlaybackControl.MediaControlGlyphs.Pause;
                        PlayPauseToolTip.Content = CommonSharedStrings.ToolTipPause;
                        break;
                    case MediaElementState.Paused:
                    case MediaElementState.Closed:
                    case MediaElementState.Stopped:
                        PlayPauseGlyph.Glyph = Controls.PlaybackControl.MediaControlGlyphs.Play;
                        PlayPauseToolTip.Content = CommonSharedStrings.ToolTipPlay;
                        break;
                }
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.Initialize(mediaElement);

            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
            Messenger.Default.Register<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
            Messenger.Default.Register<GenericMessage<List<Tuple<string, Exception>>>>(this, CommonSharedStrings.ShowLibraryScanExceptions, OnLibraryScanException);
            Messenger.Default.Register<GenericMessage<(string, string)>>(this, CommonSharedStrings.InternalToastMessage, OnToastRequested);

            Messenger.Default.Register<GenericMessage<Tuple<Type, int>>>(this, CommonSharedStrings.FrameViewNavigationIntMessageToken, OnFrameViewNavigationIntRequested);

            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            PlaybackControl.Instance.ErrorOccurred += OnPlaybackErrorOccurred;
            PlaybackControl.Instance.Player.CurrentStateChanged += OnPlaybackStateChanged;

            PlaybackControl.Instance.MobileMode = (PlaybackMode)NowPlayingStateManager.PlaybackMode;

            if (NowPlayingStateManager.IsShuffleEnabled)
            {
                PlaybackControl.Instance.EnableShuffle();
            }
            if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                StatusBar.GetForCurrentView().ForegroundColor = Colors.White;
            }
            UpdateVisibleBoundsPadding();
            ApplicationView.GetForCurrentView().VisibleBoundsChanged += OnVisibleBoundsChanged;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexStartedMessageToken, OnIndexStarted);
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexGettingFilesMessageToken, OnIndexGettingFiles);
            Messenger.Default.Unregister<GenericMessage<string>>(this, CommonSharedStrings.IndexItemAddedMessageToken, OnItemAdded);
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
            Messenger.Default.Unregister<GenericMessage<List<Tuple<string, Exception>>>>(this, CommonSharedStrings.ShowLibraryScanExceptions, OnLibraryScanException);
            Messenger.Default.Unregister<GenericMessage<(string, string)>>(this, CommonSharedStrings.InternalToastMessage, OnToastRequested);

            Messenger.Default.Unregister<GenericMessage<Tuple<Type, int>>>(this, CommonSharedStrings.FrameViewNavigationIntMessageToken, OnFrameViewNavigationIntRequested);

            Application.Current.EnteredBackground -= OnEnteredBackground;
            Application.Current.LeavingBackground -= OnLeavingBackground;
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
            PlaybackControl.Instance.ErrorOccurred -= OnPlaybackErrorOccurred;
            PlaybackControl.Instance.Player.CurrentStateChanged -= OnPlaybackStateChanged;
        }

        private void OnFrameViewNavigationIntRequested(GenericMessage<Tuple<Type, int>> obj)
        {
            ContentFrame.Navigate(obj.Content.Item1, obj.Content.Item2, new DrillInNavigationTransitionInfo());
        }

        private async void OnPlaybackErrorOccurred(object sender, (string FileName, string ErrorMessage) e)
        {
            try
            {
                var dialog = new MessageDialog(
                    string.Format(CommonSharedStrings.PlaybackErrorFormat, e.FileName, e.ErrorMessage),
                    CommonSharedStrings.Failure);
                await dialog.ShowAsync();
            }
            catch { }
        }

        private async void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            _isRunningInBackground = false;
            await Task.Delay(2000);
            UpdateBackgroundColor(_lastUseDefault);
        }

        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            _isRunningInBackground = true;
        }

        private void OnToastRequested(GenericMessage<(string, string)> message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            m_notificationToastHost.Popup(message.Content.Item1, message.Content.Item2);
        }

        private void OnLibraryScanException(GenericMessage<List<Tuple<string, Exception>>> obj)
        {
            //throw new NotImplementedException();
        }

        private async void OnIndexFinished(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    await statusBar.ProgressIndicator.HideAsync();
                }
            });
        }

        private async void OnItemAdded(GenericMessage<string> obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        await statusBar.ProgressIndicator.ShowAsync();
                        statusBar.ProgressIndicator.Text =
                            string.Format(CommonSharedStrings.LibraryFilesAddedFormat, obj.Content);
                    }
                }
            });
        }

        private async void OnIndexGettingFiles(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        await statusBar.ProgressIndicator.ShowAsync();
                        statusBar.ProgressIndicator.Text = "Getting all files";
                    }
                }
            });
        }

        private async void OnIndexStarted(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    if (statusBar != null)
                    {
                        await statusBar.ProgressIndicator.ShowAsync();
                    }
                }
            });
        }

        bool _loaded = false, _lastUseDefault = true;
        AsyncLock _backgroundColorLock = new AsyncLock();

        /// <summary>
        /// Update background color using album cover as a baseline reference.
        /// </summary>
        /// <remarks>
        /// This method contains lots of workarounds, DO NOT TOUCH THIS UNLESS YOU ABSOLUTELY KNOW WHAT YOU ARE DOING.
        /// </remarks>
        private async void UpdateBackgroundColor(bool useDefaultColor = false)
        {
            _lastUseDefault = useDefaultColor;
            if (_isRunningInBackground)
            {
                return;
            }
            using (await _backgroundColorLock.LockAsync())
            {
                if (_loaded)
                    await Task.Delay(100);
                else
                {
                    await Task.Delay(1000);
                    _loaded = true;
                }
                var isColorRetrievalApplicable = true;
                SolidColorBrush currentBrush = (SolidColorBrush)BottomArea.Background;
                SolidColorBrush candidateBrush = null;
                if (!useDefaultColor)
                {
                    var target = new RenderTargetBitmap();
                    try
                    {
                        await target.RenderAsync(Thumbnail);
                    }
                    catch
                    {
                        // workaround for RenderAsync throwing ArgumentOutOfRangeException 
                        // when target control is not visible.
                        return;
                    }


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
                            var result = Windows.UI.Color.FromArgb(a, r, g, b);
                            candidateBrush = new SolidColorBrush(result);
                        }
                    }
                }
                else
                {
                    isColorRetrievalApplicable = false;
                }

                // Fallback color for no reference items
                if (!isColorRetrievalApplicable)
                {
                    candidateBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51));
                }

                // Will not update if we have the same color as previous one
                if (currentBrush?.Color != candidateBrush.Color)
                {
                    // Animation first. Real value will be set later.
                    var colorTransitionStoryBoard = (Storyboard)Resources["TransitionColorStoryboard"];
                    ((ColorAnimation)colorTransitionStoryBoard.Children[0]).To = candidateBrush.Color;

                    // Let it fire!
                    colorTransitionStoryBoard.Begin();
                }
            }
        }

        private async void OnNowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                if (e.NewItem == null)
                {
                    return;
                }

                if (BottomArea.Visibility == Visibility.Collapsed)
                {
                    BottomArea.Visibility = Visibility.Visible;
                    var margin = ContentFrame.Margin;
                    margin.Bottom = 60;
                    ContentFrame.Margin = margin;
                }

                var metadata = PlaybackControl.Instance.Current?.File;
                if (metadata != null)
                {
                    TitleText.Text = metadata.Title;
                    ArtistText.Text = metadata.Artist;
                    // Update cover, if available
                    Thumbnail.ThumbnailTag = new ThumbnailTag
                    {
                        Fallback = "Album,AlbumPlaceholder",
                        AlbumName = metadata.Album,
                        ArtistName = metadata.Artist,
                        ThumbnailPath = metadata.Path
                    };
                }
            });
        }

        private void OnPlayPauseClick(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.PlayOrPause();
        }

        private void Thumbnail_ImageChanged(object sender, Light.Controls.MediaThumbnailImageChangedEventArgs e)
        {
            UpdateBackgroundColor(e.IsDefaultImage);
        }

        private void CheckPlaybackControlVisibility(MobileBasePage page)
        {
            if (BottomArea != null)
            {
                if (page == null || page.ShowPlaybackControl)
                {
                    BottomArea.Height = 60;
                    if (BottomArea.Visibility == Visibility.Visible)
                    {
                        var margin = ContentFrame.Margin;
                        margin.Bottom = 60;
                        ContentFrame.Margin = margin;
                    }
                    else
                    {
                        var margin = ContentFrame.Margin;
                        margin.Bottom = 0;
                        ContentFrame.Margin = margin;
                    }
                }
                else
                {
                    BottomArea.Height = 0;
                    var margin = ContentFrame.Margin;
                    margin.Bottom = 0;
                    ContentFrame.Margin = margin;
                }
            }
        }

        private void OnContentFrameNavigated(object sender, NavigationEventArgs e)
        {
            var page = e.Content as MobileBasePage;
            if (page == null || page.ReserveSpaceForStatusBar)
            {
                var margin = ContentFrame.Margin;
                margin.Top = 24;
                ContentFrame.Margin = margin;
            }
            else
            {
                var margin = ContentFrame.Margin;
                margin.Top = 0;
                ContentFrame.Margin = margin;
            }

            CheckPlaybackControlVisibility(page);
        }

        double _lastVelocity;
        double _lastHeight = 0;
        private bool _isRunningInBackground = false;

        private void OnControlPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            ShowNowPlayingAnimation();
            ShowNowPlaying();
        }

        private void OnControlPanelManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            _lastHeight = BottomArea.ActualHeight;
            PlaybackControlPanelBorder.Visibility = Visibility.Collapsed;
            BottomArea.ManipulationDelta += OnControlPanelManipulationDelta;
            ShowNowPlaying();
        }

        private void OnControlPanelManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _lastVelocity = e.Velocities.Linear.Y;
            var result = _lastHeight - e.Delta.Translation.Y;
            BottomArea.Height = _lastHeight = Math.Max(result, 60);
            var op = (_lastHeight - 60) / (ActualHeight - 60);
            PlaybackControlPanel.Opacity = 1 - op;
            NowPlayingPage.Opacity = op;
        }

        private void CheckManipulationResult()
        {
            var height = BottomArea.Height;
            if ((height > ActualHeight / 3 && _lastVelocity < 0)
                || -_lastVelocity > 1.0)
            {
                ShowNowPlayingAnimation();
            }
            else
            {
                HideNowPlayingAnimation();
                HideNowPlaying();
            }
        }

        private void OnControlPanelManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            PlaybackControlPanelBorder.Visibility = Visibility.Visible;
            BottomArea.ManipulationDelta -= OnControlPanelManipulationDelta;
            CheckManipulationResult();
        }

        private void OnTopPanelManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            EventPanel.Visibility = Visibility.Visible;
            PlaybackControlPanelBorder.Visibility = Visibility.Collapsed;
            _lastHeight = BottomArea.ActualHeight;
            BottomArea.Height = BottomArea.ActualHeight;
            RelativePanel.SetAlignTopWithPanel(BottomArea, false);
            TopPanel.ManipulationDelta += OnTopPanelManipulationDelta;
        }

        private void OnTopPanelManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            _lastVelocity = e.Velocities.Linear.Y;
            var result = _lastHeight - e.Delta.Translation.Y;
            BottomArea.Height = _lastHeight = Math.Max(result, 60);
            var op = (_lastHeight - 60) / (ActualHeight - 60);
            PlaybackControlPanel.Opacity = 1 - op;
            NowPlayingPage.Opacity = op;
        }

        private void CheckManipulationBackResult()
        {
            var height = BottomArea.Height;
            if ((height < ActualHeight * 2 / 3 && _lastVelocity > 0)
                || _lastVelocity > 1.0)
            {
                HideNowPlayingAnimation();
                HideNowPlaying();
            }
            else
            {
                ShowNowPlayingAnimation();
            }
        }

        private void OnTopPanelManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            TopPanel.ManipulationDelta -= OnTopPanelManipulationDelta;
            CheckManipulationBackResult();
        }
    }
}
