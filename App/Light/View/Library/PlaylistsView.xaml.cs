using GalaSoft.MvvmLight;
using Light.Common;
using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Native;
using Light.Shell;
using Light.Utilities;
using Light.ViewModel.Library.Detailed;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Foundation.Metadata;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.View.Library
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class PlaylistsView : Page
    {
        private ObservableCollection<Playlist> Playlists = new ObservableCollection<Playlist>();
        private bool _initialized = false;
        private NavigationHelper _navigation;
        public PlaylistsView()
        {
            InitializeComponent();
            PlaylistView.MapDetails = playlist =>
            {
                if (playlist is Playlist p)
                {
                    return new PlaylistDetailViewModel(p.Title);
                }
                else
                {
                    return null;
                }
            };
            _navigation = new NavigationHelper(this);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigation.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _navigation.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!_initialized)
            {
                await PlaylistManager.Instance.InitializeAsync(CommonSharedStrings.FavoritesLocalizedText);
                _initialized = true;
            }
            Playlists.Clear();
            foreach (var list in PlaylistManager.Instance.GetAllPlaylists())
            {
                Playlists.Add(list);
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

        private async void OnNewPlaylistClicked(object sender, TappedRoutedEventArgs e)
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

        private async void OnMenuPlayClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            PlaybackControl.Instance.Stop();
            PlaybackControl.Instance.Clear();
            await PlaybackControl.Instance.AddFile(
                from item
                in playlist.Items
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void OnMenuAddClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            await PlaybackControl.Instance.AddFile(
                from item
                in playlist.Items
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void OnMenuDeleteClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            var confirmMessage = new MessageDialog(
                playlist == PlaylistManager.Instance.FavoriteList ?
                CommonSharedStrings.FavoriteDeleteDescription :
                string.Format(
                    CommonSharedStrings.PlaylistDeleteDescription,
                    playlist.Title),
                playlist == PlaylistManager.Instance.FavoriteList ?
                CommonSharedStrings.FavoriteClearTitle :
                CommonSharedStrings.PlaylistDeleteTitle)
            {
                DefaultCommandIndex = 0,
                CancelCommandIndex = 1
            };
            confirmMessage.Commands.Add(new UICommand(
                CommonSharedStrings.ConfirmString,
                new UICommandInvokedHandler(DeletePlaylist),
                playlist));
            confirmMessage.Commands.Add(new UICommand(CommonSharedStrings.CancelString));
            await confirmMessage.ShowAsync();
        }

        private async void DeletePlaylist(IUICommand command)
        {
            var playlist = command.Id as Playlist;
            try
            {
                await PlaylistManager.Instance.RemoveListAsync(playlist.Title);
            }
            catch (ArgumentException ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.InternalErrorTitle);
                await dialog.ShowAsync();
            }
        }

        private async void OnMenuShareClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            List<StorageFile> files = new List<StorageFile>();
            foreach (var file in playlist.Items)
            {
                if (await NativeMethods.GetStorageFileFromPathAsync(file.Path) is StorageFile f)
                    files.Add(f);
            }
            if (files.Count == 0)
                return;
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            shareService.Title = playlist.Title;
            shareService.Description = string.Empty;
            shareService.AddFiles(files);
            shareService.ShowShareUI();
        }

        private async void OnMenuRenameClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            var newName = await FieldEditor.ShowAsync(
                playlist.Title,
                CommonSharedStrings.RenameString,
                CommonSharedStrings.NewNameEmptyPrompt,
                "[^ ]");
            if (newName == null)
                return;
            try
            {
                await PlaylistManager.Instance.RenameAsync(playlist.Title, newName);
            }
            catch (ArgumentException ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.InternalErrorTitle);
                await dialog.ShowAsync();
            }
        }

        private async void OnExportM3uClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            try
            {
                FileSavePicker picker = new FileSavePicker()
                {
                    SuggestedFileName = playlist.Title,
                    DefaultFileExtension = ".m3u8",
                    SuggestedStartLocation = PickerLocationId.MusicLibrary
                };
                picker.FileTypeChoices.Add(CommonSharedStrings.M3u, new List<string> { ".m3u8", ".m3u" });
                var file = await picker.PickSaveFileAsync();
                if (file == null)
                    return;
                using (var stream = await file.OpenStreamForWriteAsync())
                using (var sw = new StreamWriter(stream, Encoding.UTF8))
                {
                    sw.Write(M3u.Export(playlist.Items));
                }
                MessagePrompt(CommonSharedStrings.PlaylistSaved);
            }
            catch (Exception ex)
            {
                MessagePrompt(ex.Message, CommonSharedStrings.FailedToSavePlaylist);
            }
        }

        private async void OnExportWplClicked(object sender, RoutedEventArgs e)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            try
            {
                var fileName = await FieldEditor.ShowAsync($"{playlist.Title}.wpl",
                    CommonSharedStrings.ChooseFileName,
                    CommonSharedStrings.ValidFilenameRequired,
                    @"^(?!((con|prn|aux)((\.[^\\/:*?<>|" + "\"" + @"]{1,3}$)|$))|[\s\.])[^\\/:*?<>|" + "\"" + @"]{1,254}$");
                if (fileName == null)
                    return;
                var picker = new FolderPicker()
                {
                    SuggestedStartLocation = PickerLocationId.MusicLibrary,
                    ViewMode = PickerViewMode.List
                };
                picker.FileTypeFilter.Add(".wpl");
                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                    return;
                await PlaylistManager.Instance.ExportAsync(playlist, folder, fileName);
                MessagePrompt(CommonSharedStrings.PlaylistSaved);
            }
            catch (Exception ex)
            {
                MessagePrompt(ex.Message, CommonSharedStrings.FailedToSavePlaylist);
            }
        }

        private async void MessagePrompt(string content, string title = null)
        {
            if (title == null)
                await new MessageDialog(content).ShowAsync();
            else
                await new MessageDialog(content, title).ShowAsync();
        }

        private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            args.DragUI.SetContentFromDataPackage();
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            args.Data.SetText(DragHelper.Add(playlist));
        }

        private void OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            var playlist = (sender as FrameworkElement).DataContext as Playlist;
            DragHelper.Remove(playlist);
        }

        // A workaround for setting CanDrag="True" disables MasterDetailView selection.
        private void OnPlaylistItemTapped(object sender, TappedRoutedEventArgs e)
        {
            PlaylistView.SelectedItem = (sender as FrameworkElement).DataContext;
        }

        PlaylistDetailViewModel _prev;

        private void OnDetailsViewDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (args.NewValue == _prev)
            {
                return;
            }

            if (_prev != null)
            {
                _prev.Cleanup();
                _prev = null;
            }

            if (args.NewValue is PlaylistDetailViewModel p)
            {
                _prev = p;
            }
        }

        private void OnEditToggleButtonClicked(object sender, RoutedEventArgs e)
        {
            var vm = (sender as FrameworkElement).DataContext as PlaylistDetailViewModel;
            vm.PlaylistListViewSelectionMode =
                vm.IsEditToggleButtonChecked ?
                    ListViewSelectionMode.Multiple :
                    ListViewSelectionMode.None;
        }

        private void OnPlaylistListViewSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var vm = (sender as FrameworkElement).DataContext as PlaylistDetailViewModel;
            vm.SelectedItems = (sender as ListView).SelectedItems;
        }
    }
}
