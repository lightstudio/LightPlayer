using Light.Common;
using Light.Core;
using Light.Flyout;
using Light.Lyrics;
using Light.Lyrics.External;
using Light.Lyrics.Model;
using Light.Managed.Tools;
using Light.Phone.ViewModel;
using LightLrcComponent;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.Display;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;

namespace Light.Phone.View
{
    partial class MobileFrameView
    {
        private DisplayRequest _displayRequest = new DisplayRequest();
        private DispatcherTimer _nowPlayingTimer;
        private MobileNowPlayingViewModel _nowPlayingViewModel;
        private bool _isInNowPlaying = false;

        private async void CheckPlayStatus()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                switch (PlaybackControl.Instance.Player.CurrentState)
                {
                    case MediaElementState.Playing:
                    case MediaElementState.Opening:
                    case MediaElementState.Buffering:
                        _nowPlayingTimer.Start();
                        _nowPlayingViewModel.PlayPause = MobileNowPlayingViewModel.Pause;
                        break;
                    case MediaElementState.Paused:
                    case MediaElementState.Closed:
                    case MediaElementState.Stopped:
                        _nowPlayingTimer.Stop();
                        _nowPlayingViewModel.PlayPause = MobileNowPlayingViewModel.Play;
                        break;
                }
            });
        }

        private void InitializeNowPlaying()
        {
            _nowPlayingViewModel = new MobileNowPlayingViewModel();
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;
        }

        private void UpdateNowPlayingStatus()
        {
            CheckPlayStatus();
            _nowPlayingViewModel.UpdateNowPlaying(PlaybackControl.Instance.Current);
            ShuffleButton.Opacity = NowPlayingStateManager.IsShuffleEnabled ? 1 : 0.5;
            if (NowPlayingStateManager.PlaybackMode == (int)PlaybackMode.SingleTrackLoop)
            {
                _nowPlayingViewModel.Repeat = MobileNowPlayingViewModel.SingleTrackLoop;
            }
            else
            {
                _nowPlayingViewModel.Repeat = MobileNowPlayingViewModel.AutoRepeat;
            }
        }

        private void DisableManipulation()
        {
            EventPanel.ManipulationMode = ManipulationModes.None;
            TopPanel.ManipulationMode = ManipulationModes.None;
        }

        private void EnableManipulation()
        {
            EventPanel.ManipulationMode = ManipulationModes.TranslateY;
            TopPanel.ManipulationMode = ManipulationModes.TranslateY;
        }

        private void ShowNowPlayingAnimation()
        {
            var sb = Resources["ShowNowPlayingStoryboard"] as Storyboard;
            var animation = sb.Children[0] as DoubleAnimationUsingKeyFrames;
            var keyframe = animation.KeyFrames[0] as SplineDoubleKeyFrame;
            keyframe.Value = NowPlayingPage.ActualHeight;

            var cpAnimation = sb.Children[1] as DoubleAnimation;
            cpAnimation.From = PlaybackControlPanel.Opacity;
            cpAnimation.To = 0.0;

            var npAnimation = sb.Children[2] as DoubleAnimation;
            npAnimation.From = NowPlayingPage.Opacity;
            npAnimation.To = 1.0;

            DisableManipulation();
            sb.Begin();
            sb.Completed += OnShowNowPlayingStoryboardCompleted;
        }

        // page loaded
        private async void OnShowNowPlayingStoryboardCompleted(object sender, object e)
        {
            EnableManipulation();
            var sb = sender as Storyboard;
            sb.Completed -= OnShowNowPlayingStoryboardCompleted;
            RelativePanel.SetAlignTopWithPanel(BottomArea, true);
            BottomArea.Height = double.NaN;
            _isInNowPlaying = true;
            EventPanel.Visibility = Visibility.Collapsed;

            if (await LrcAutoSearch())
            {
                await Task.Delay(200);
                LrcPresenter.ForceRefresh();
            }
        }

        private void ShowNowPlaying()
        {
            // Add event handler
            PlaybackControl.Instance.Player.CurrentStateChanged += NowPlayingOnPlaybackStateChanged;
            PlaylistManager.Instance.FavoriteChanged += NowPlayingOnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged += NowPlayingOnNowPlayingItemChanged;
            Application.Current.EnteredBackground += OnNowPlayingEnteredBackground;
            Application.Current.LeavingBackground += OnNowPlayingLeavingBackground;
            LrcPresenter.Player = PlaybackControl.Instance.Player;
            // Start timer
            _nowPlayingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _nowPlayingTimer.Tick += NowPlayingTimerOnTick;
            // Update status
            UpdateNowPlayingStatus();
            // Check displayrequest
            if (StateGroup.CurrentState == LyricsState)
            {
                _displayRequest.RequestActive();
            }
        }

        private async void OnNowPlayingLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            PlaybackControl.Instance.Player.CurrentStateChanged += NowPlayingOnPlaybackStateChanged;
            PlaylistManager.Instance.FavoriteChanged += NowPlayingOnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged += NowPlayingOnNowPlayingItemChanged;
            _nowPlayingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _nowPlayingTimer.Tick += NowPlayingTimerOnTick;
            UpdateNowPlayingStatus();
            if (StateGroup.CurrentState == LyricsState)
            {
                _displayRequest.RequestActive();
            }
            if (await LrcAutoSearch())
            {
                await Task.Delay(200);
                LrcPresenter.ForceRefresh();
            }
        }

        private void OnNowPlayingEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            PlaybackControl.Instance.Player.CurrentStateChanged -= NowPlayingOnPlaybackStateChanged;
            PlaylistManager.Instance.FavoriteChanged -= NowPlayingOnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged -= NowPlayingOnNowPlayingItemChanged;
            _nowPlayingTimer.Tick -= NowPlayingTimerOnTick;
            _nowPlayingTimer.Stop();
            if (StateGroup.CurrentState == LyricsState)
            {
                _displayRequest.RequestRelease();
            }
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_isInNowPlaying)
            {
                e.Handled = true;
                HideNowPlayingAnimation();
                HideNowPlaying();
            }
        }

        private void HideNowPlayingAnimation()
        {
            EventPanel.Visibility = Visibility.Visible;
            PlaybackControlPanelBorder.Visibility = Visibility.Visible;
            BottomArea.Height = BottomArea.ActualHeight;
            RelativePanel.SetAlignTopWithPanel(BottomArea, false);

            var sb = Resources["HideNowPlayingStoryboard"] as Storyboard;
            var animation = sb.Children[0] as DoubleAnimationUsingKeyFrames;
            var keyframe = animation.KeyFrames[0] as SplineDoubleKeyFrame;
            keyframe.Value = 60;

            var cpAnimation = sb.Children[1] as DoubleAnimation;
            cpAnimation.From = PlaybackControlPanel.Opacity;

            var npAnimation = sb.Children[2] as DoubleAnimation;
            npAnimation.From = NowPlayingPage.Opacity;

            DisableManipulation();
            sb.Begin();
            sb.Completed += OnHideNowPlayingStoryboardCompleted;
        }

        private void OnHideNowPlayingStoryboardCompleted(object sender, object e)
        {
            var sb = sender as Storyboard;
            sb.Completed -= OnHideNowPlayingStoryboardCompleted;
            EnableManipulation();
            CheckPlaybackControlVisibility(ContentFrame.Content as MobileBasePage);
        }

        private void HideNowPlaying()
        {
            if (StateGroup.CurrentState == LyricsState)
            {
                _displayRequest.RequestRelease();
            }
            // Remove event handler
            PlaybackControl.Instance.Player.CurrentStateChanged -= NowPlayingOnPlaybackStateChanged;
            PlaylistManager.Instance.FavoriteChanged -= NowPlayingOnFavoriteChanged;
            PlaybackControl.Instance.NowPlayingChanged -= NowPlayingOnNowPlayingItemChanged;
            Application.Current.EnteredBackground -= OnNowPlayingEnteredBackground;
            Application.Current.LeavingBackground -= OnNowPlayingLeavingBackground;
            // Stop timer
            _nowPlayingTimer.Tick -= NowPlayingTimerOnTick;
            _nowPlayingTimer.Stop();
            _isInNowPlaying = false;
        }

        private void NowPlayingOnFavoriteChanged(object sender, FavoriteChangedEventArgs e)
        {
            switch (e.Change)
            {
                case FavoriteChangeType.Added:
                    if (e.Item.Equals(PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File)))
                    {
                        _nowPlayingViewModel.IsInFavorite = true;
                    }
                    break;
                case FavoriteChangeType.Removed:
                    if (e.Item.Equals(PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File)))
                    {
                        _nowPlayingViewModel.IsInFavorite = false;
                    }
                    break;
                case FavoriteChangeType.Unknown:
                    _nowPlayingViewModel.CheckFavorite();
                    break;
            }
        }

        private void NowPlayingOnPlaybackStateChanged(object sender, RoutedEventArgs e)
        {
            CheckPlayStatus();
        }

        private void NowPlayingTimerOnTick(object sender, object e)
        {
            _nowPlayingViewModel.NowPlayingTime = PlaybackControl.Instance.Player.Position;
        }

        private void OnPlayPauseButtonClick(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.PlayOrPause();
        }

        private void OnPrevClick(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.Prev();
        }

        private void OnNextClick(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.Next();
        }

        private void OnShuffleClick(object sender, RoutedEventArgs e)
        {
            if (NowPlayingStateManager.IsShuffleEnabled)
            {
                PlaybackControl.Instance.DisableShuffle();
                ShuffleButton.Opacity = 0.5;
                NowPlayingStateManager.IsShuffleEnabled = false;
            }
            else
            {
                PlaybackControl.Instance.EnableShuffle();
                ShuffleButton.Opacity = 1;
                NowPlayingStateManager.IsShuffleEnabled = true;
            }
        }

        private void OnLoopClick(object sender, RoutedEventArgs e)
        {
            if (NowPlayingStateManager.PlaybackMode == (int)PlaybackMode.SingleTrackLoop)
            {
                _nowPlayingViewModel.Repeat = MobileNowPlayingViewModel.AutoRepeat;
                PlaybackControl.Instance.Mode = PlaybackMode.ListLoop;
                NowPlayingStateManager.PlaybackMode = (int)PlaybackMode.ListLoop;
            }
            else
            {
                _nowPlayingViewModel.Repeat = MobileNowPlayingViewModel.SingleTrackLoop;
                PlaybackControl.Instance.Mode = PlaybackMode.SingleTrackLoop;
                NowPlayingStateManager.PlaybackMode = (int)PlaybackMode.SingleTrackLoop;
            }
        }

        private async void OnFavoriteClick(object sender, RoutedEventArgs e)
        {
            if (_nowPlayingViewModel.IsInFavorite)
            {
                await PlaylistManager.Instance.RemoveFromFavoriteAsync(
                    PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File));
            }
            else
            {
                await PlaylistManager.Instance.AddToFavoriteAsync(
                    PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File));
            }
        }

        private async void OnAddToFavoriteClick(object sender, RoutedEventArgs e)
        {
            await PlaylistManager.Instance.AddToFavoriteAsync(
                PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File));
        }

        private async void OnRemoveFromFavoriteClick(object sender, RoutedEventArgs e)
        {
            await PlaylistManager.Instance.RemoveFromFavoriteAsync(
                PlaylistItem.FromMediaFile(_nowPlayingViewModel.CurrentItem.File));
        }

        private void OnMenuClick(object sender, RoutedEventArgs e)
        {
            var s = sender as FrameworkElement;
            var flyout = (MenuFlyout)FlyoutBase.GetAttachedFlyout(s);
            flyout.ShowAt(s, new Point(0, 0));
        }

        private void OnLyricsPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            AlbumImageStateTrigger.IsActive = true;
            LyricsStateTrigger.IsActive = false;
        }

        private void OnImagePanelTapped(object sender, TappedRoutedEventArgs e)
        {
            LyricsStateTrigger.IsActive = true;
            AlbumImageStateTrigger.IsActive = false;
        }

        private void OnShowLyricsMenuClick(object sender, RoutedEventArgs e)
        {
            LyricsStateTrigger.IsActive = true;
            AlbumImageStateTrigger.IsActive = false;
        }

        private void OnHideLyricsMenuClick(object sender, RoutedEventArgs e)
        {
            AlbumImageStateTrigger.IsActive = true;
            LyricsStateTrigger.IsActive = false;
        }

        private void OnNowPlayingPlayPauseButtonClick(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.PlayOrPause();
        }

        private async void HideAndNavigate(Type targetType, object param = null)
        {
            HideNowPlayingAnimation();
            HideNowPlaying();
            await Task.Delay(350);
            ContentFrame.Navigate(targetType, param);
        }

        private async void NowPlayingOnNowPlayingItemChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, async () =>
            {
                _nowPlayingViewModel.UpdateNowPlaying(e.NewItem);
                await LrcAutoSearch();
            });
        }

        private async Task<bool> LrcAutoSearch()
        {
            _nowPlayingViewModel.LrcMissing = false;
            _nowPlayingViewModel.LrcSearchBusy = true;
            LrcPresenter.Lyrics = null;
            var _ttitle = _nowPlayingViewModel.Title;
            var _tartist = _nowPlayingViewModel.Artist;
            _nowPlayingViewModel.LrcCandidates = new ObservableCollection<ExternalLrcInfo>();
            ParsedLrc lrc = null;
            try
            {
                lrc = await LyricsAgent.FetchLyricsAsync(
                    _nowPlayingViewModel.Title, _nowPlayingViewModel.Artist, _nowPlayingViewModel.LrcCandidates,
                    LyricsAgent.BuildLyricsFilename(_nowPlayingViewModel.Title, _nowPlayingViewModel.Artist));
            }
            catch
            {

            }
            if (_ttitle == _nowPlayingViewModel.Title && _tartist == _nowPlayingViewModel.Artist)
            {
                LrcPresenter.Lyrics = lrc;
                _nowPlayingViewModel.LrcSearchBusy = false;
                _nowPlayingViewModel.LrcMissing = lrc == null || lrc.Sentences.Count == 0;
                return true;
            }
            return false;
        }

        private async void OnImportLyricsClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(CommonSharedStrings.LrcFileSuffix);
            picker.FileTypeFilter.Add(CommonSharedStrings.TxtFileSuffix);
            picker.CommitButtonText = CommonSharedStrings.ManualSelectLyricButtonText;
            var lyricFile = await picker.PickSingleFileAsync();
            if (lyricFile == null)
                return;
            var lrc = await LyricsAgent.ImportLyricsAsync(
                _nowPlayingViewModel.Title,
                _nowPlayingViewModel.Artist,
                lyricFile);
            _nowPlayingViewModel.LrcMissing = lrc == null;
            LrcPresenter.Lyrics = lrc;
        }

        private async void LrcSearch()
        {
            try
            {
                var modifyFlyout = new LyricManualSelectionFlyout();
                modifyFlyout.LrcSelected += ModifyFlyoutOnItemSaved;
                await modifyFlyout.ShowAsync(_nowPlayingViewModel.Title, _nowPlayingViewModel.Artist, _nowPlayingViewModel.LrcCandidates);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        private void ModifyFlyoutOnItemSaved(object sender, LrcSelectedEventArgs e)
        {
            ((LyricManualSelectionFlyout)sender).LrcSelected -= ModifyFlyoutOnItemSaved;
            LrcPresenter.Lyrics = e.Lrc;
            _nowPlayingViewModel.LrcMissing = e.Lrc == null;
        }

        private async void OnAddLyricsExtensionClick(Hyperlink sender, HyperlinkClickEventArgs args)
        {
            try
            {
                FileOpenPicker picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };
                picker.FileTypeFilter.Add(CommonSharedStrings.JavaScriptFileFormatSuffix);

                var files = await picker.PickMultipleFilesAsync();
                foreach (var file in files)
                {
                    var name = file.DisplayName;

                    var text = await FileIO.ReadTextAsync(file);
                    SourceScriptManager.AddScript(name, text);
                }
                await LrcAutoSearch();
            }
            catch (SecurityException)
            {
                // Ignore, notify user
            }
            catch (COMException)
            {
                // Ignore, notify user
            }
            catch (FileNotFoundException)
            {
                // Ignore, notify user
            }
        }

        private void OnSearchForLyricsMenuClick(object sender, RoutedEventArgs e)
        {
            LrcSearch();
        }

        private void OnGoBackClick(object sender, RoutedEventArgs e)
        {
            HideNowPlayingAnimation();
            HideNowPlaying();
        }

        private void OnAddToPlaylistClick(object sender, RoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Current?.File != null)
            {
                PlaylistPicker.Pick(PlaylistItem.FromMediaFile(
                    PlaybackControl.Instance.Current.File));
            }
        }

        private async void OnDetailsClick(object sender, RoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Current?.File != null)
            {
                try
                {
                    var file = await StorageFile.GetFileFromPathAsync(PlaybackControl.Instance.Current.File.Path);
                    await MediaFilePropertiesDialog.ShowFilePropertiesViewAsync(file);
                }
                catch
                {
                    var dialog = new MessageDialog("Failed to find file.", "Error");
                    await dialog.ShowAsync();
                }
            }
        }

        private void OnNowPlayingListClick(object sender, RoutedEventArgs e)
        {
            HideAndNavigate(typeof(MobileNowPlayingListView));
        }

        private void OnCurrentStateChanged(object sender, VisualStateChangedEventArgs e)
        {
            if (e.NewState == LyricsState)
            {
                _displayRequest.RequestActive();
            }
            else
            {
                _displayRequest.RequestRelease();
            }
        }

        private void OnGoToAlbumClick(object sender, RoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Current?.File?.RelatedAlbumId != null)
            {
                HideAndNavigate(typeof(MobileAlbumDetailView), PlaybackControl.Instance.Current.File.RelatedAlbumId);
            }
        }

        private void OnGoToArtistClick(object sender, RoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Current?.File?.RelatedAlbumId != null)
            {
                HideAndNavigate(typeof(MobileArtistDetailView), PlaybackControl.Instance.Current.File.RelatedArtistId);
            }
        }
    }
}
