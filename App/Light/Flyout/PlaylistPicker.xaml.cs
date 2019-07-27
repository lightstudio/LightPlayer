using GalaSoft.MvvmLight;
using Light.Core;
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
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Flyout
{
    public sealed partial class PlaylistPicker : ContentDialog
    {
        private IEnumerable<PlaylistItem> _items;
        static public async void Pick(PlaylistItem item)
        {
            PlaylistPicker dialog = new PlaylistPicker(item);
            await dialog.ShowAsync();
        }
        static public async void Pick(IEnumerable<PlaylistItem> items)
        {
            PlaylistPicker dialog = new PlaylistPicker(items);
            await dialog.ShowAsync();
        }
        public ObservableCollection<Playlist> Playlists { get; }
        public bool PrimaryButtonEnabled
        {
            get
            {
                return _selectedIndex != -1;
            }
        }

        private int _selectedIndex = -1;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                if (_selectedIndex == value)
                    return;
                _selectedIndex = value;
                Bindings.Update();
            }
        }
        public PlaylistPicker(PlaylistItem item) : this(new PlaylistItem[] { item }) { }
        public PlaylistPicker(IEnumerable<PlaylistItem> items)
        {
            InitializeComponent();
            Playlists = new ObservableCollection<Playlist>();
            foreach (var list in PlaylistManager.Instance.GetAllPlaylists())
            {
                Playlists.Add(list);
            }
            _items = items;
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var list = PlaylistManager.Instance.GetAllPlaylists()[SelectedIndex];
            foreach (var item in _items)
            {
                list.Items.Add(item);
            }
            await PlaylistManager.Instance.AddOrUpdatePlaylistAsync(list);
        }
    }
}
