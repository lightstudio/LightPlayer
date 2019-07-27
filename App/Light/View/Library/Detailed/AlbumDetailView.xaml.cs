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
    /// Album detailed page.
    /// </summary>
    public sealed partial class AlbumDetailView : Page
    {
        private readonly NavigationHelper _navigationHelper;
        private AlbumDetailViewModel _vm;
        private int _id;
        /// <summary>
        /// Class constructor.
        /// </summary>
        public AlbumDetailView()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter != null)
            {
                DataContext = _vm = new AlbumDetailViewModel(_id = (int) e.Parameter);
            }

            _navigationHelper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Handle navigate from events and cleanup all used variables.
        /// </summary>
        /// <param name="e">NavigateFrom event arguments, used by common navigation helper.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _vm.Cleanup();
            _navigationHelper.OnNavigatedFrom(e);
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
            if (_vm != null)
            {
                await _vm.LoadDataAsync();
            }
        }

        private async void OnAddIconClick(object sender, RoutedEventArgs e)
        {
            var elem = await EntityRetrievalExtensions.GetAlbumByIdAsync(_id);
            elem.Play(false, false);
        }

        private async void OnPlayIconClick(object sender, RoutedEventArgs e)
        {
            var elem = await EntityRetrievalExtensions.GetAlbumByIdAsync(_id);
            elem.Play(true, false);
        }
    }
}