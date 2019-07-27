using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Light.Common;
using Light.ViewModel.Library.Detailed;
using Light.Managed.Database.Entities;
using Light.Core;

namespace Light.View.Library.Detailed
{
    /// <summary>
    /// Artist Detailed View.
    /// </summary>
    public sealed partial class ArtistDetailView : Page
    {
        private readonly NavigationHelper _helper;
        private ArtistDetailViewModel _vm;
        private int _id;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ArtistDetailView()
        {
            InitializeComponent();
            _helper = new NavigationHelper(this);
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                _id = (int)e.Parameter;
                DataContext = _vm = new ArtistDetailViewModel((int)e.Parameter, Frame);
            }
           
            _helper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Handle navigate from events and cleanup all used variables.
        /// </summary>
        /// <param name="e">NavigateFrom event arguments, used by common navigation helper.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _vm.Cleanup();
            _helper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Handler for Page loaded event.
        /// Used to load data.
        /// </summary>
        /// <param name="sender">This page.</param>
        /// <param name="e">Unused.</param>
        private async void PageLoaded(object sender, RoutedEventArgs e)
        {
            await _vm.LoadDataAsync();
        }

        private void OnShuffleAllTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _id.GetArtistById().Shuffle();
        }

        private void OnAddIconClick(object sender, RoutedEventArgs e)
        {
            var album = (sender as Button).DataContext as DbAlbum;
            album.Play(false, false);
        }

        private void OnPlayIconClick(object sender, RoutedEventArgs e)
        {
            var album = (sender as Button).DataContext as DbAlbum;
            album.Play(true, false);
        }

        private void OnGridSizeChanged(object sender, SizeChangedEventArgs e)
        {
            RootGrid.RowDefinitions[0].Height = new GridLength(e.NewSize.Width / 3);
            ArtistImage.Margin = new Thickness(0, -e.NewSize.Width / 9, 0, 0);
        }
    }
}