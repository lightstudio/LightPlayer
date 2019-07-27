using Light.Core;
using Light.Model;
using Light.Phone.ViewModel;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.ApplicationModel;
using Light.Flyout;
using Light.View.Core;
using Windows.UI.Popups;
using Light.Common;
using Light.Managed.Database;
using System.Linq;
using Light.Managed.Tools;
using Light.View.Feedback;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileHomeView : MobileBasePage
    {
        const int MaxHistoryItems = 5;

        private MobileHomeViewModel ViewModel { get { return (MobileHomeViewModel)DataContext; } }

        public override bool ReserveSpaceForStatusBar => false;

        public MobileHomeView()
        {
            this.InitializeComponent();
        }

        private void OnNextPlayItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
            if (item != PlaybackControl.Instance.Current)
            {
                PlaybackControl.Instance.SetIndex(PlaybackControl.Instance.Items.IndexOf(item));
            }

            if (PlaybackControl.Instance.Player.CurrentState != MediaElementState.Playing &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Buffering &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Opening)
            {
                PlaybackControl.Instance.Play();
            }
        }

        private async void OnRecentItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;

            var idx = PlaybackControl.Instance.Items.IndexOf(item);

            if (idx != -1)
            {
                PlaybackControl.Instance.SetIndex(idx);
            }
            else
            {
                await PlaybackControl.Instance.AddToNextAndPlay(item);
            }

            if (PlaybackControl.Instance.Player.CurrentState != MediaElementState.Playing &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Buffering &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Opening)
            {
                PlaybackControl.Instance.Play();
            }
        }

        private void OnBackgroundImagePanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            BackgroundImagePanel.Height = e.NewSize.Width * 3 / 4;
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MobileSearchView));
        }

        private void CheckOpenLibraryPanelVisibility()
        {
            if (NowPlayingList.Visibility == Visibility.Collapsed &&
                RecentlyList.Visibility == Visibility.Collapsed)
            {
                OpenLibraryPanel.Visibility = Visibility.Visible;
            }
            else
            {
                OpenLibraryPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void OnLeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored += OnNowPlayingRestored;
            PlaybackHistoryManager.Instance.NewEntryAdded += OnNewHistoryEntryAdded;

            ViewModel.UpdateBindings();

            ViewModel.NowPlayingTracks = new Utilities.NextTrackSubset(Dispatcher, 5);
            ViewModel.NowPlayingTracks.CollectionChanged += OnUpcomingChanged;

            var history = PlaybackHistoryManager.Instance.GetHistory(MaxHistoryItems);
            ViewModel.HistoryItems = new ObservableCollection<MusicPlaybackItem>(history);
            CheckUpcomingVisibility();
            CheckHistoryVisibility();
        }

        private void OnEnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            ViewModel.NowPlayingTracks.Close();
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored -= OnNowPlayingRestored;
            PlaybackHistoryManager.Instance.NewEntryAdded -= OnNewHistoryEntryAdded;
            ViewModel.NowPlayingTracks.CollectionChanged -= OnUpcomingChanged;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored += OnNowPlayingRestored;
            PlaybackHistoryManager.Instance.NewEntryAdded += OnNewHistoryEntryAdded;


            if (DataContext == null)
            {
                DataContext = new MobileHomeViewModel();
                await PlaybackControl.Instance.RestoreAsync();
            }
            else
            {
                ViewModel.UpdateBindings();
            }

            ViewModel.NowPlayingTracks = new Utilities.NextTrackSubset(Dispatcher, 5);
            ViewModel.NowPlayingTracks.CollectionChanged += OnUpcomingChanged;

            var history = PlaybackHistoryManager.Instance.GetHistory(MaxHistoryItems);
            ViewModel.HistoryItems = new ObservableCollection<MusicPlaybackItem>(history);
            CheckUpcomingVisibility();
            CheckHistoryVisibility();


            Application.Current.EnteredBackground += OnEnteredBackground;
            Application.Current.LeavingBackground += OnLeavingBackground;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NowPlayingTracks.Close();
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored -= OnNowPlayingRestored;
            PlaybackHistoryManager.Instance.NewEntryAdded -= OnNewHistoryEntryAdded;
            ViewModel.NowPlayingTracks.CollectionChanged -= OnUpcomingChanged;

            Application.Current.EnteredBackground -= OnEnteredBackground;
            Application.Current.LeavingBackground -= OnLeavingBackground;
        }

        private async void OnNewHistoryEntryAdded(object sender, MusicPlaybackItem e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
               () =>
               {
                   ViewModel.HistoryItems.Insert(0, e);
                   for (int i = ViewModel.HistoryItems.Count - 1; i >= MaxHistoryItems; i--)
                   {
                       ViewModel.HistoryItems.RemoveAt(i);
                   }
                   CheckHistoryVisibility();
               });
        }

        private async void OnNowPlayingRestored(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                CheckUpcomingVisibility();
            });
        }

        private void OnUpcomingChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            CheckUpcomingVisibility();
        }

        private void CheckUpcomingVisibility()
        {
            NowPlayingList.Visibility = NowPlayingHeader.Visibility = ViewModel.NowPlayingTracks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            CheckOpenLibraryPanelVisibility();
        }

        private void CheckHistoryVisibility()
        {
            RecentlyHeader.Visibility = RecentlyList.Visibility = ViewModel.HistoryItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            CheckOpenLibraryPanelVisibility();
        }

        private async void OnNowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    CheckUpcomingVisibility();
                    ViewModel.UpdateBindings();
                });
        }

        private void OnRecentlyTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(RecentlyListenedView));
        }

        private void OnSongsTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LibrarySongsView), CommonItemType.Song);
        }

        private void OnAlbumsTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LibrarySongsView), CommonItemType.Album);
        }

        private void OnArtistsTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(LibrarySongsView), CommonItemType.Artist);
        }

        private void OnOpenLibraryClick(object sender, RoutedEventArgs e)
        {
            MainPivot.SelectedIndex = 1;
        }

        private async void OnShuffleAllTapped(object sender, TappedRoutedEventArgs e)
        {
            if (GlobalLibraryCache.CachedDbMediaFile == null)
            {
                await GlobalLibraryCache.LoadMediaAsync();
            }
            var s = from x 
                    in GlobalLibraryCache.CachedDbMediaFile.Shuffle(new Random())
                    select MusicPlaybackItem.CreateFromMediaFile(x);
            PlaybackControl.Instance.Clear();
            await PlaybackControl.Instance.AddAndSetIndexAt(s, 0);
        }

        //private void OnAddFilesTapped(object sender, TappedRoutedEventArgs e)
        //{

        //}

        private void OnRefreshLibraryTapped(object sender, TappedRoutedEventArgs e)
        {
            SharedUtils.ConfirmRefreshLibrary();
        }

        private async void OnFeedbackTapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                // Launch SIUF interface
                await FeedbackView.LaunchFeedbackAsync(null);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        private void OnSettingsTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(SettingsView));
        }

        private async void OnAboutTapped(object sender, TappedRoutedEventArgs e)
        {
            var flyout = new AboutFlyout();
            await flyout.ShowAsync();
        }

        private void OnPlaylistTapped(object sender, TappedRoutedEventArgs e)
        {
            Frame.Navigate(typeof(MobilePlaylistsView));
        }
    }
}
