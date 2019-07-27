using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Shell;
using Light.ViewModel.Library;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Library
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomeView : Page
    {
        private HomeViewModel ViewModel { get { return (HomeViewModel)DataContext; } }
        private ObservableCollection<DbMediaFile> AllFiles;
        private readonly NavigationHelper _navigationHelper;
        private Random _random = new Random();

        private static IEnumerable<T> Shuffle<T>(IEnumerable<T> source, Random rng)
        {
            var buffer = source.ToList();
            for (int i = 0; i < buffer.Count; i++)
            {
                int j = rng.Next(i, buffer.Count);
                yield return buffer[j];

                buffer[j] = buffer[i];
            }
        }

        public HomeView()
        {
            InitializeComponent();
            AllFiles = new ObservableCollection<DbMediaFile>();

            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
            Messenger.Default.Register<GenericMessage<DbMediaFile[]>>(this, "NewItemAdded", OnItemAdded);
            _navigationHelper = new NavigationHelper(this);
        }

        private async void OnIndexFinished(MessageBase obj)
        {
            await Task.Run(async () =>
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                    {
                        AllFiles = new ObservableCollection<DbMediaFile>(Shuffle(context.MediaFiles, _random));
                        Bindings.Update();
                        EmptyIndicator.Visibility = AllFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                    });
                }
            });
        }

        private async void OnItemAdded(GenericMessage<DbMediaFile[]> obj)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    if (AllFiles.Count == 0)
                    {
                        EmptyIndicator.Visibility = Visibility.Collapsed;
                    }
                    foreach (var item in obj.Content)
                        AllFiles.Insert(_random.Next(AllFiles.Count), item);
                });
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
            DesktopTitleViewConfiguration.SetTitleBarText(CommonSharedStrings.Home);
            base.OnNavigatedTo(e);

            if (AllFiles.Count == 0 && !LibraryService.IsIndexing)
            {
                await Task.Run(async () =>
                {
                    using (var scope = ApplicationServiceBase.App.GetScope())
                    using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                    {
                        var files = Shuffle(context.MediaFiles, _random);
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                        {
                            foreach (var file in files) AllFiles.Add(file);
                            EmptyIndicator.Visibility = AllFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
                        });
                    }
                });
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            PlaybackControl.Instance.NowPlayingChanged += OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored += OnNowPlayingRestored;
            PlaybackControl.Instance.Player.CurrentStateChanged += OnPlaybackStateChanged;
            PlaybackHistoryManager.Instance.NewEntryAdded += OnNewHistoryEntryAdded;
            SizeChanged += OnViewSizeChanged;
            RootGrid.RowDefinitions[0].Height = new GridLength(ActualWidth / 3);

            EmptyIndicator.Visibility = AllFiles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

            if (PlaybackControl.Instance.Current != null)
            {
                ShowNowPlaying();
            }

            if (DataContext == null)
            {
                DataContext = new HomeViewModel();
                await PlaybackControl.Instance.RestoreAsync();
            }
            else
            {
                ViewModel.UpdateBindings();
                Bindings.Update();
                OnPlaybackStateChanged(null, null);
            }
            ViewModel.NextTracks = new Utilities.NextTrackSubset(Dispatcher, 15);
            ViewModel.NextTracks.CollectionChanged += OnUpcomingChanged;
            var history = PlaybackHistoryManager.Instance.GetHistory(20);
            ViewModel.HistoryItems = new ObservableCollection<MusicPlaybackItem>(history);
            m_expMrHistoryPanel.Visibility = HistoryList.Visibility = ViewModel.HistoryItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
            m_expUpcomingPanel.Visibility = NextPlayList.Visibility = ViewModel.NextTracks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OnUpcomingChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            m_expUpcomingPanel.Visibility = NextPlayList.Visibility = ViewModel.NextTracks.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void OnNewHistoryEntryAdded(object sender, MusicPlaybackItem e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    ViewModel.HistoryItems.Insert(0, e);
                    m_expMrHistoryPanel.Visibility = HistoryList.Visibility = ViewModel.HistoryItems.Count == 0 ? Visibility.Collapsed : Visibility.Visible;
                });
        }

        private void ShowAllMusic()
        {
            RootGrid.Visibility = Visibility.Collapsed;
            SecondaryGrid.Visibility = Visibility.Visible;
        }

        private void ShowNowPlaying()
        {
            RootGrid.Visibility = Visibility.Visible;
            SecondaryGrid.Visibility = Visibility.Collapsed;
        }

        private async void OnNowPlayingRestored(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                () =>
                {
                    if (!PlaybackControl.Instance.ItemLoaded ||
                        PlaybackControl.Instance.MediaLoadFailed)
                    {
                        HomeViewLoadProgress.IsActive = false;
                        ShowAllMusic();
                        ViewModel.UpdateBindings();
                    }
                });
        }

        private void OnPlaybackStateChanged(object sender, RoutedEventArgs e)
        {
            switch (PlaybackControl.Instance.Player.CurrentState)
            {
                case MediaElementState.Opening:
                case MediaElementState.Buffering:
                case MediaElementState.Playing:
                    NowPlayingButtonText.Text = CommonSharedStrings.NowPlayingUpper;
                    break;
                case MediaElementState.Paused:
                case MediaElementState.Closed:
                case MediaElementState.Stopped:
                    NowPlayingButtonText.Text = CommonSharedStrings.ContinuePlaylistUpper;
                    break;
            }
        }

        private void OnViewSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RootGrid.RowDefinitions[0].Height = new GridLength(e.NewSize.Width / 3);
            ArtistImage.Margin = new Thickness(0, -e.NewSize.Width / 9, 0, 0);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NextTracks.Close();
            PlaybackControl.Instance.NowPlayingChanged -= OnNowPlayingChanged;
            PlaybackControl.Instance.NowPlayingRestored -= OnNowPlayingRestored;
            PlaybackControl.Instance.Player.CurrentStateChanged -= OnPlaybackStateChanged;
            SizeChanged -= OnViewSizeChanged;
            PlaybackHistoryManager.Instance.NewEntryAdded -= OnNewHistoryEntryAdded;
            ViewModel.NextTracks.CollectionChanged -= OnUpcomingChanged;
        }

        private async void OnNowPlayingChanged(object sender, NowPlayingChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High,
                () =>
                {
                    HomeViewLoadProgress.IsActive = false;
                    ShowNowPlaying();
                    ViewModel.UpdateBindings(e.NewItem);
                });
        }

        private void OnOpenPlaylistClick(object sender, RoutedEventArgs e)
        {
            Messenger.Default.Send(new MessageBase(), "SplitViewOpen");
        }

        private void OnContinuePlaylistTapped(object sender, TappedRoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Player.CurrentState != MediaElementState.Playing ||
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Buffering)
            {
                PlaybackControl.Instance.Play();
            }
            Messenger.Default.Send(new GenericMessage<bool>(true), "ShowNowPlayingView");
        }

        private void OnNextPlayItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
            PlaybackControl.Instance.SetIndex(PlaybackControl.Instance.Items.IndexOf(item));
            if (PlaybackControl.Instance.Player.CurrentState != MediaElementState.Playing &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Buffering &&
                PlaybackControl.Instance.Player.CurrentState != MediaElementState.Opening)
            {
                PlaybackControl.Instance.Play();
            }
        }

        private async void OnHistoryItemTapped(object sender, TappedRoutedEventArgs e)
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

        private async void OnAllMusicItemTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as DbMediaFile;
            var idx = AllFiles.IndexOf(item);

            var items = AllFiles.Select(c => MusicPlaybackItem.CreateFromMediaFile(c));
            await PlaybackControl.Instance.AddAndSetIndexAt(items, idx);
        }

        // Workaround bug #596: https://ligstd.visualstudio.com/Light%20Player/_workitems/edit/596/
        private void OnButtonTapped(object sender, TappedRoutedEventArgs e)
        {
            e.Handled = true;
        }
    }
}
