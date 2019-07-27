using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Controls;
using Light.Core;
using Light.Managed.Database;
using Light.Model;
using Light.Phone.ViewModel;
using Light.Utilities;
using Light.Utilities.Grouping;
using Light.View.Library;
using Light.ViewModel.Library;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class LibrarySongsView : MobileBasePage
    {
        static readonly Dictionary<CommonItemType, string> ResourceName = new Dictionary<CommonItemType, string>
        {
            { CommonItemType.Song, "MusicListTemplate" },
            { CommonItemType.Album, "AlbumArtistListTemplate" },
            { CommonItemType.Artist, "AlbumArtistListTemplate" }
        };

        internal LibraryViewModel ViewModel => (LibraryViewModel)DataContext;

        private CancellationTokenSource _cts;

        public ObservableCollection<IndexerComparerPair> SortingOptions = new ObservableCollection<IndexerComparerPair>();

        IPageGroupingStateManager _groupState;

        private int _sortingMethod = -1;
        public int SortingMethod
        {
            get
            {
                return _sortingMethod;
            }
            set
            {
                if (_sortingMethod == value)
                    return;
                _sortingMethod = value;
                if (_sortingMethod == -1)
                    return;
                var cp = _groupState.PopulateAvailablePairs()[value];
                Messenger.Default.Send(new GenericMessage<IndexerComparerPair>(cp), CommonSharedStrings.GroupingChangeMessageToken);
                _groupState.SetLastUsedPair(cp);
            }
        }

        public LibrarySongsView()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            _cts = new CancellationTokenSource();
            if (ViewModel == null)
            {
                var type = (CommonItemType)e.Parameter;
                ContentGridView.ItemTemplate = (DataTemplate)Resources[ResourceName[type]];
                if (type == CommonItemType.Song)
                {
                    var collection = Resources["AlternatingColorBehavior"] as BehaviorCollection;
                    collection.Attach(ContentGridView);
                    _groupState = new PageGroupingStateManager<CommonGroupedListView>(type);
                }
                else
                {
                    _groupState = new PageGroupingStateManager<CommonGroupedGridView>(type);
                }
                // Reload options
                SortingOptions.Clear();
                var options = _groupState.PopulateAvailablePairs();
                foreach (var option in options)
                {
                    SortingOptions.Add(option);
                }

                var lastUsedOption = _groupState.GetLastUsedPair();
                var elem = SortingOptions.Where(i => i.Identifier == lastUsedOption.Identifier).ToList();
                if (elem.Any())
                {
                    SortingMethod = SortingOptions.IndexOf(elem.FirstOrDefault());
                }
                DataContext = new LibraryViewModel(new DataObjects.GroupedViewNavigationArgs(type, lastUsedOption));

                GroupedCvs.Source = ViewModel.GroupedItems.Items;
                GroupedCvs.IsSourceGrouped = true;

                await ViewModel.LoadDataAsync(_cts.Token);
            }
            Messenger.Default.Register<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);

            if (_previousAnimatedControl != null)
            {
                var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("image");
                imageAnimation?.TryStart(_previousAnimatedControl);
                _previousAnimatedControl = null;
            }
        }

        private async void OnIndexFinished(MessageBase obj)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await ViewModel.LoadDataAsync(_cts.Token));
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Messenger.Default.Unregister<MessageBase>(this, CommonSharedStrings.IndexFinishedMessageToken, OnIndexFinished);
            base.OnNavigatedFrom(e);

            _cts.Cancel();
            _cts.Dispose();
            // Some small tricks.
            // Only preserve navigation cache when navigating to detailed view.
            if (e.SourcePageType != typeof(MobileAlbumDetailView) &&
                e.SourcePageType != typeof(MobileArtistDetailView))
            {
                ViewModel.Cleanup();
                NavigationCacheMode = NavigationCacheMode.Disabled;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
        }

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

        private async void OnShuffleAllClick(object sender, RoutedEventArgs e)
        {
            if (GlobalLibraryCache.CachedDbMediaFile == null)
            {
                await GlobalLibraryCache.LoadMediaAsync();
            }
            var s = from x in Shuffle(GlobalLibraryCache.CachedDbMediaFile, new Random())
                    select MusicPlaybackItem.CreateFromMediaFile(x);
            Core.PlaybackControl.Instance.Clear();
            await Core.PlaybackControl.Instance.AddAndSetIndexAt(s, 0);
        }

        private MediaThumbnail _previousAnimatedControl;

        private void OnListItemInfoPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            var control = sender as PhoneListItemControl;
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(
                "image",
                control.ThumbnailControl);
            _previousAnimatedControl = control.ThumbnailControl;
            var ctx = control.DataContext as CommonViewItemModel;
            if (ctx.Type == CommonItemType.Album)
            {
                Frame.Navigate(
                    typeof(MobileAlbumDetailView),
                    ctx.InternalDbEntityId,
                    new SuppressNavigationTransitionInfo());
            }
            else if (ctx.Type == CommonItemType.Artist)
            {
                Frame.Navigate(
                    typeof(MobileArtistDetailView),
                    ctx.InternalDbEntityId,
                    new SuppressNavigationTransitionInfo());
            }
        }
    }
}
