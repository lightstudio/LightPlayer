using Light.Core;
using Light.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using Windows.Phone.UI.Input;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileNowPlayingListView : MobileBasePage
    {
        public static readonly DependencyProperty ItemDragGripVisibilityProperty =
            DependencyProperty.Register(nameof(ItemDragGripVisibility),
                typeof(Visibility), typeof(MobileNowPlayingListView), new PropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register(nameof(Playlist), typeof(object), typeof(MobileNowPlayingListView),
                new PropertyMetadata(default(object)));
        public override bool ShowPlaybackControl => false;

        private void OnBackPressed(object sender, BackPressedEventArgs e)
        {
            if (EditToggleButton.IsChecked ?? false)
            {
                PlayItemsListView.SelectionMode = ListViewSelectionMode.None;
                ItemDragGripVisibility = Visibility.Collapsed;
                EditToggleButton.IsChecked = false;
                e.Handled = true;
            }
        }

        private void OnEditToggleButtonClicked(object sender, RoutedEventArgs e)
        {
            if (EditToggleButton.IsChecked ?? false)
            {
                PlayItemsListView.SelectionMode = ListViewSelectionMode.Multiple;
                ItemDragGripVisibility = Visibility.Visible;
            }
            else
            {
                PlayItemsListView.SelectionMode = ListViewSelectionMode.None;
                ItemDragGripVisibility = Visibility.Collapsed;
            }
        }
        public object Playlist
        {
            get
            {
                return GetValue(PlaylistProperty);
            }
            set
            {
                SetValue(PlaylistProperty, value);
                PlayItemsListView.ItemsSource = value;
            }
        }
        public Visibility ItemDragGripVisibility
        {
            get { return (Visibility)GetValue(ItemDragGripVisibilityProperty); }
            set { SetValue(ItemDragGripVisibilityProperty, value); }
        }

        private async void OnMediaPlaybackItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (!(EditToggleButton.IsChecked ?? false))
            {
                await PlaybackControl.Instance.PlayAt(
                    (Playlist as IList).IndexOf(
                        (sender as Grid).DataContext as MusicPlaybackItem));
            }
        }
        public MobileNowPlayingListView()
        {
            this.InitializeComponent();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.BackPressed += OnBackPressed;
            }
            if (Playlist == null)
            {
                Playlist = PlaybackControl.Instance.Items;
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.Phone.UI.Input.HardwareButtons"))
            {
                HardwareButtons.BackPressed -= OnBackPressed;
            }
            base.OnNavigatedFrom(e);
        }

        private async void OnDeleteClick(object sender, RoutedEventArgs e)
        {
            var listCopy = PlayItemsListView.SelectedItems.ToArray();
            foreach (var entity in listCopy)
            {
                await PlaybackControl.Instance.RemoveAsync((MusicPlaybackItem)entity);
            }
            ExitEditMode();
        }

        private void ExitEditMode()
        {
            EditToggleButton.IsChecked = false;
            PlayItemsListView.SelectionMode = ListViewSelectionMode.None;
        }

        private async void OnPlayClick(object sender, RoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
            await PlaybackControl.Instance.PlayAt(item);
        }

        private async void OnDeleteMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (PlayItemsListView.SelectedItems != null &&
                PlayItemsListView.SelectedItems.Count > 0)
            {
                var listCopy = PlayItemsListView.SelectedItems.ToArray();
                foreach (var entity in listCopy)
                {
                    await PlaybackControl.Instance.RemoveAsync((MusicPlaybackItem)entity);
                }
                ExitEditMode();
            }
            else
            {
                var item = (sender as FrameworkElement).DataContext as MusicPlaybackItem;
                await PlaybackControl.Instance.RemoveAsync(item);
            }
        }

        private void OnItemHolding(object sender, HoldingRoutedEventArgs e)
        {
            if (PlaybackControl.Instance.Current ==
                (sender as FrameworkElement).DataContext)
            {
                e.Handled = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var idx = PlaybackControl.Instance.Items.IndexOf(
                PlaybackControl.Instance.Current);
            if (idx != -1)
            {
                PlayItemsListView.ScrollIntoView(PlayItemsListView.Items[idx], ScrollIntoViewAlignment.Leading);
            }
        }
    }
}
