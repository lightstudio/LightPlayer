using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Light.Common;
using Light.ViewModel.Library.Detailed;
using Windows.UI.Xaml;
using Light.Core;
using System.Linq;
using Windows.Foundation.Metadata;

namespace Light.View.Library.Detailed
{
    /// <summary>
    /// Playlist Detailed View.
    /// </summary>
    public sealed partial class PlaylistDetailView : Page
    {
        private PlaylistDetailViewModel _vm;
        private readonly NavigationHelper _navigationHelper;
        
        private void OnEditToggleButtonClicked(object sender, RoutedEventArgs e) =>
            PlaylistListView.SelectionMode =
                (EditToggleButton.IsChecked ?? false) ?
                    ListViewSelectionMode.Multiple :
                    ListViewSelectionMode.None;

        private bool CanExportPlaylist => ApiInformation.IsApiContractPresent("Windows.Media.Playlists.PlaylistsContract", 1);

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PlaylistDetailView()
        {
            this.InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string)
            {
                _vm = new PlaylistDetailViewModel((string)e.Parameter);
                DataContext = _vm;
                _navigationHelper.OnNavigatedTo(e);
            }
            base.OnNavigatedTo(e);
        }

        /// <summary>
        /// Handle navigate from events and cleanup all used variables.
        /// </summary>
        /// <param name="e">NavigateFrom event arguments, used by common navigation helper.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
            _vm.Cleanup();
        }

        private void OnDeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            foreach (PlaylistItem item in PlaylistListView.SelectedItems.ToArray())
            {
                _vm.TempPlaylist.Items.Remove(item);
            }
            EditToggleButton.IsChecked = false;
            PlaylistListView.SelectionMode = ListViewSelectionMode.None;
        }
    }
}
