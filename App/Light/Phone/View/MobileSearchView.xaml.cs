using Light.Controls;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.Model;
using Light.Utilities;
using Light.ViewModel.Library;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileSearchView : MobileBasePage
    {
        public SearchViewModel ViewModel => DataContext as SearchViewModel;

        private MediaThumbnail _previousAnimatedControl;
        private bool _loaded = false;

        public MobileSearchView()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (ViewModel == null)
            {
                DataContext = new SearchViewModel();
                await LibrarySearchUtils.LoadLibraryCacheAsync();
            }

            if (_previousAnimatedControl != null)
            {
                var imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("image");
                imageAnimation?.TryStart(_previousAnimatedControl);
                _previousAnimatedControl = null;
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                NavigationCacheMode = NavigationCacheMode.Disabled;
            }
            base.OnNavigatingFrom(e);
        }

        private void OnSearchBoxTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var keyword = SearchBox.Text;

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput &&
                keyword != string.Empty)
            {
                ViewModel.UpdateSuggestions(keyword);
            }
            else
            {
                ViewModel.ClearSuggestions();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_loaded)
            {
                SearchBox.Focus(FocusState.Keyboard);
                _loaded = true;
            }
        }

        private void OnSearchBoxSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            var item = args.SelectedItem as SearchResultModel;
            switch (item.ItemType)
            {
                case CommonItemType.Album:
                    var album = item.Entity as DbAlbum;
                    Frame.Navigate(typeof(MobileAlbumDetailView), album.Id, new SlideNavigationTransitionInfo());
                    break;
                case CommonItemType.Artist:
                    var artist = item.Entity as DbArtist;
                    Frame.Navigate(typeof(MobileArtistDetailView), artist.Id, new SlideNavigationTransitionInfo());
                    break;
                case CommonItemType.Song:
                    ViewModel.DoQuery();
                    break;
            }
        }

        private void OnSearchBoxQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion == null)
            {
                ViewModel.DoQuery();
            }
        }

        private void OnAlbumSearchResultTapped(object sender, TappedRoutedEventArgs e)
        {
            var panel = sender as StackPanel;
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(
                "image",
                panel.Children[0]);
            _previousAnimatedControl = panel.Children[0] as MediaThumbnail;
            var item = (panel.DataContext as SearchResultModel).Entity as DbAlbum;
            Frame.Navigate(typeof(MobileAlbumDetailView), item.Id, new SuppressNavigationTransitionInfo());
        }

        private void OnArtistSearchResultTapped(object sender, TappedRoutedEventArgs e)
        {
            var panel = sender as StackPanel;
            ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(
                "image",
                panel.Children[0]);
            _previousAnimatedControl = panel.Children[0] as MediaThumbnail;
            var item = (panel.DataContext as SearchResultModel).Entity as DbArtist;
            Frame.Navigate(typeof(MobileArtistDetailView), item.Id, new SuppressNavigationTransitionInfo());
        }
    }
}
