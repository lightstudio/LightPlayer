using Light.Common;
using Light.Core;
using Light.Flyout;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
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
    public sealed partial class MobilePlaylistsView : MobileBasePage
    {
        private ObservableCollection<Playlist> Playlists = new ObservableCollection<Playlist>();
        private bool _initialized = false;

        public override bool ShowPlaybackControl => false;

        public MobilePlaylistsView()
        {
            this.InitializeComponent();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                await PlaylistManager.Instance.InitializeAsync(CommonSharedStrings.FavoritesLocalizedText);
                foreach (var list in PlaylistManager.Instance.GetAllPlaylists())
                {
                    Playlists.Add(list);
                }
                _initialized = true;
            }
            Bindings.Update();
            PlaylistManager.Instance.PlaylistChanged += OnPlaylistChanged;
        }

        private void OnPlaylistChanged(object sender, PlaylistChangedEventArgs e)
        {
            switch (e.Action)
            {
                case PlaylistChangeAction.Add:
                    Playlists.Add(e.Playlist);
                    break;
                case PlaylistChangeAction.Remove:
                    Playlists.Remove(e.Playlist);
                    break;
                default:
                    return;
            }
            Bindings.Update();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            PlaylistManager.Instance.PlaylistChanged -= OnPlaylistChanged;
        }

        private async void OnNewPlaylistClicked(object sender, RoutedEventArgs e)
        {
            var name = await FieldEditor.ShowAsync(
                CommonSharedStrings.PlaylistDefaultname,
                CommonSharedStrings.NewPlaylistString,
                CommonSharedStrings.NewNameEmptyPrompt,
                "[^ ]");
            if (name == null)
                return;
            try
            {
                await PlaylistManager.Instance.CreateBlankPlaylistAsync(name);
            }
            catch (ArgumentException ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.InternalErrorTitle);
                await dialog.ShowAsync();
            }
        }

        private void OnPlaylistTapped(object sender, TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as Playlist;
            Frame.Navigate(typeof(MobilePlaylistDetailView), item.Title);
        }
    }
}
