using Light.Core;
using Light.ViewModel.Library.Detailed;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobilePlaylistDetailView : MobileBasePage
    {
        private PlaylistDetailViewModel _vm;

        private void OnEditToggleButtonClicked(object sender, RoutedEventArgs e) =>
            PlaylistListView.SelectionMode =
                (EditToggleButton.IsChecked ?? false) ?
                    ListViewSelectionMode.Multiple :
                    ListViewSelectionMode.None;

        private bool CanExportPlaylist => ApiInformation.IsApiContractPresent("Windows.Media.Playlists.PlaylistsContract", 1);

        public override bool ShowPlaybackControl => false;
        public MobilePlaylistDetailView()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (e.Parameter is string)
            {
                _vm = new PlaylistDetailViewModel((string)e.Parameter);
                DataContext = _vm;
            }
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
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
