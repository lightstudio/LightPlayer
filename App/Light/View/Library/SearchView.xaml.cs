using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Managed.Database.Entities;
using Light.Model;
using Light.View.Library.Detailed;
using Light.ViewModel.Library;
using System;
using System.Collections.Generic;
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
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.View.Library
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchView : Page
    {
        private readonly NavigationHelper _navigationHelper;
        public SearchViewModel ViewModel => DataContext as SearchViewModel;
        public SearchView()
        {
            _navigationHelper = new NavigationHelper(this);
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (ViewModel == null)
            {
                DataContext = new SearchViewModel();
            }

            if (ViewModel.SearchKeyword != (string)e.Parameter)
            {
                ViewModel.SearchKeyword = (string)e.Parameter;
                ViewModel.DoQuery();
            }

            _navigationHelper.OnNavigatedTo(e);
            Messenger.Default.Register<GenericMessage<string>>(this, "RequestSearch", OnSearchRequested);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Cleanup();
            _navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        private void OnSearchRequested(GenericMessage<string> obj)
        {
            ViewModel.SearchKeyword = obj.Content;
            ViewModel.DoQuery();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (e.NavigationMode == NavigationMode.Back)
            {
                NavigationCacheMode = NavigationCacheMode.Disabled;
            }
            Messenger.Default.Unregister<GenericMessage<string>>(this, "RequestSearch", OnSearchRequested);
            base.OnNavigatingFrom(e);
        }

        private void OnAlbumSearchResultTapped(object sender, TappedRoutedEventArgs e)
        {
            var album = ((sender as StackPanel).DataContext as SearchResultModel).Entity as DbAlbum;
            Messenger.Default.Send(
                new GenericMessage<Tuple<Type, int>>(
                    new Tuple<Type, int>(typeof(AlbumDetailView), album.Id)),
                        CommonSharedStrings.FrameViewNavigationIntMessageToken);
        }

        private void OnArtistSearchResultTapped(object sender, TappedRoutedEventArgs e)
        {
            var artist = ((sender as StackPanel).DataContext as SearchResultModel).Entity as DbArtist;
            Messenger.Default.Send(
                new GenericMessage<Tuple<Type, int>>(
                    new Tuple<Type, int>(typeof(ArtistDetailView), artist.Id)),
                        CommonSharedStrings.FrameViewNavigationIntMessageToken);
        }
    }
}
