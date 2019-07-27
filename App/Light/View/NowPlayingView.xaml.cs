using Light.Common;
using Light.Core;
using Light.Lyrics;
using Light.Lyrics.External;
using Light.Managed.Database;
using Light.Utilities;
using Light.View.Core;
using Light.View.Library.Detailed;
using Light.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Light.View
{
    public sealed partial class NowPlayingView : Page
    {
        private NowPlayingViewModel _vm => (NowPlayingViewModel)DataContext;
        private DispatcherTimer _timer;
        private readonly NavigationHelper _navigationHelper;
        public NowPlayingView()
        {
            _navigationHelper = new NavigationHelper(this);
            InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (await _vm.LrcAutoSearch())
            {
                await Task.Delay(200);
                LrcPresenter.ForceRefresh();
            }
        }

        private async void CheckPlayStatus()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                switch (PlaybackControl.Instance.Player.CurrentState)
                {
                    case MediaElementState.Playing:
                    case MediaElementState.Opening:
                    case MediaElementState.Buffering:
                        _timer.Start();
                        break;
                    case MediaElementState.Paused:
                    case MediaElementState.Closed:
                    case MediaElementState.Stopped:
                        _timer.Stop();
                        break;
                }
            });
        }

        private void OnPlaybackStateChanged(object sender, RoutedEventArgs e)
        {
            CheckPlayStatus();
        }

        private void TimerOnTick(object sender, object e)
        {
            _vm.NowPlayingTime = PlaybackControl.Instance.Player.Position;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (DataContext == null)
            {
                DataContext = new NowPlayingViewModel(Dispatcher, LrcPresenter);
                LrcPresenter.Player = PlaybackControl.Instance.Player;
            }
            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;
            _vm.RegisterEvents();
            _navigationHelper.OnNavigatedTo(e);
            _vm.NextItems = new NextTrackSubset(Dispatcher, 15);

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.5) };
            _timer.Tick += TimerOnTick;
            PlaybackControl.Instance.Player.CurrentStateChanged += OnPlaybackStateChanged;
            CheckPlayStatus();

            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Application.Current.EnteredBackground -= OnEnteredBackground;
            Application.Current.LeavingBackground -= OnLeavingBackground;
            _vm.UnregsterEvents();
            _navigationHelper.OnNavigatedFrom(e);
            _timer.Tick -= TimerOnTick;
            _timer.Stop();
            _vm.NextItems.Close();


            PlaybackControl.Instance.Player.CurrentStateChanged -= OnPlaybackStateChanged;
            base.OnNavigatedFrom(e);
        }

        private async void OnLeavingBackground(object sender, Windows.ApplicationModel.LeavingBackgroundEventArgs e)
        {
            _vm.RegisterEvents();
            if (await _vm.LrcAutoSearch())
            {
                await Task.Delay(200);
                LrcPresenter.ForceRefresh();
            }
        }

        private void OnEnteredBackground(object sender, Windows.ApplicationModel.EnteredBackgroundEventArgs e)
        {
            _vm.UnregsterEvents();
        }

        private void OnStackPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Touch)
            {
                _vm.PlayCommand.Execute((sender as StackPanel).DataContext);
            }
        }

        private void OnStackPanelDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch)
            {
                _vm.PlayCommand.Execute((sender as StackPanel).DataContext);
            }
        }

        private void OnLyricsPointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (LrcPresenter.AllowScroll &&
                e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                LrcPresenter.AllowScroll = false;
                LrcPresenter.ForceRefresh();
            }
        }

        private void OnLyricsPointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                LrcPresenter.AllowScroll = true;
            }
        }

        private void OnLyricsRightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (LrcPresenter.AllowScroll &&
                e.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                LrcPresenter.AllowScroll = false;
                LrcPresenter.ForceRefresh();
            }
        }

        private void OnArtistNameTapped(object sender, TappedRoutedEventArgs e)
        {
            var metadata = PlaybackControl.Instance.Current?.File;
            if (!string.IsNullOrWhiteSpace(metadata?.Artist))
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var query = from c in context.Artists
                                where c.Name == metadata.Artist
                                select c;
                    var f = query.FirstOrDefault();

                    if (f != null)
                    {
                        Frame.Navigate(
                            typeof(ArtistDetailView),
                            f.Id,
                            new DrillInNavigationTransitionInfo());
                    }
                }
            }
        }

        private void OnAlbumNameTapped(object sender, TappedRoutedEventArgs e)
        {
            var metadata = PlaybackControl.Instance.Current?.File;
            if (!string.IsNullOrWhiteSpace(metadata?.Album))
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var query = from c in context.Albums
                                where c.Title == metadata.Album
                                select c;
                    var f = query.FirstOrDefault();
                    if (f != null)
                    {
                        Frame.Navigate(
                            typeof(AlbumDetailView),
                            f.Id,
                            new DrillInNavigationTransitionInfo());
                    }
                }
            }
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
                await _vm.LrcAutoSearch();
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
                _vm.NowPlayingTitle,
                _vm.NowPlayingArtist,
                lyricFile);
            _vm.LrcMissing = lrc == null;
            LrcPresenter.Lyrics = lrc;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            // Workaround: AdaptiveTrigger cannot handle the scenario when 
            // now playing list panel is open because it only depends on 
            // window size.
            var s = e.NewSize.Width;
            if (s > 1100)
            {
                if (!MaxWidthTrigger.IsActive)
                {
                    MinWidthTrigger.IsActive = false;
                    MidWidthTrigger.IsActive = false;
                    MaxWidthTrigger.IsActive = true;
                }
            }
            else if (s > 750)
            {
                if (!MidWidthTrigger.IsActive)
                {
                    MinWidthTrigger.IsActive = false;
                    MidWidthTrigger.IsActive = true;
                    MaxWidthTrigger.IsActive = false;
                }
            }
            else
            {
                if (!MinWidthTrigger.IsActive)
                {
                    MinWidthTrigger.IsActive = true;
                    MidWidthTrigger.IsActive = false;
                    MaxWidthTrigger.IsActive = false;
                }
            }
        }

        private void OnNextPlayItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
            PlaybackControl.Instance.SetIndex(PlaybackControl.Instance.Items.IndexOf(item));
            if (PlaybackControl.Instance.Player.CurrentState != MediaElementState.Playing ||
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Buffering)
            {
                PlaybackControl.Instance.Play();
            }
        }

        private void OnOffsetUpClick(object sender, RoutedEventArgs e)
        {
            LrcPresenter.UpdateOffset(TimeSpan.FromSeconds(-1));
        }
        private void OnOffsetDownClick(object sender, RoutedEventArgs e)
        {
            LrcPresenter.UpdateOffset(TimeSpan.FromSeconds(1));
        }
    }
}
