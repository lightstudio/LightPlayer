using Light.Common;
using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Native;
using Light.Shell;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace Light.Controls
{
    public sealed partial class PlaylistListItemControl : UserControl
    {
        private bool CanExportPlaylist => ApiInformation.IsApiContractPresent("Windows.Media.Playlists.PlaylistsContract", 1);
        public Playlist Playlist
        {
            get { return (Playlist)GetValue(PlaylistProperty); }
            set { SetValue(PlaylistProperty, value); }
        }

        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register(
                "Playlist",
                typeof(Playlist),
                typeof(PlaylistListItemControl),
                new PropertyMetadata(default(Playlist)));

        public string Title => ((Playlist)DataContext)?.Title;

        public string Subtitle
        {
            get
            {
                if (Playlist == null)
                    return null;
                switch (Playlist.Items.Count)
                {
                    case 0:
                        return CommonSharedStrings.PlaylistNoItemSubtitle;
                    case 1:
                        return CommonSharedStrings.PlaylistSingleItemSubtitle;
                    default:
                        return string.Format(
                            CommonSharedStrings.PlaylistSubtitle,
                            Playlist.Items.Count);
                }
            }
        }

        private bool IsFavorite
        {
            get
            {
                return Playlist == PlaylistManager.Instance.FavoriteList;
            }
        }

        private string DeleteText
        {
            get
            {
                return IsFavorite ?
                    CommonSharedStrings.ClearString :
                    CommonSharedStrings.DeleteString;
            }
        }

        public PlaylistListItemControl()
        {
            this.InitializeComponent();
        }
        private async void OnMenuPlayClicked(object sender, RoutedEventArgs e)
        {
            Core.PlaybackControl.Instance.Stop();
            Core.PlaybackControl.Instance.Clear();
            await Core.PlaybackControl.Instance.AddFile(
                from item
                in Playlist.Items
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void OnMenuAddClicked(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                from item
                in Playlist.Items
                select MusicPlaybackItem.CreateFromMediaFile(
                    item.ToMediaFile()));
        }

        private async void OnMenuDeleteClicked(object sender, RoutedEventArgs e)
        {
            var confirmMessage = new MessageDialog(
                Playlist == PlaylistManager.Instance.FavoriteList ?
                CommonSharedStrings.FavoriteDeleteDescription :
                string.Format(
                    CommonSharedStrings.PlaylistDeleteDescription,
                    Title),
                Playlist == PlaylistManager.Instance.FavoriteList ?
                CommonSharedStrings.FavoriteClearTitle :
                CommonSharedStrings.PlaylistDeleteTitle)
            {
                DefaultCommandIndex = 0,
                CancelCommandIndex = 1
            };
            confirmMessage.Commands.Add(new UICommand(
                CommonSharedStrings.ConfirmString, new UICommandInvokedHandler(DeletePlaylist)));
            confirmMessage.Commands.Add(new UICommand(CommonSharedStrings.CancelString));
            await confirmMessage.ShowAsync();
        }

        private async void DeletePlaylist(IUICommand command)
        {
            try
            {
                await PlaylistManager.Instance.RemoveListAsync(Playlist.Title);
            }
            catch (ArgumentException ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.InternalErrorTitle);
                await dialog.ShowAsync();
            }
        }

        private async void OnMenuShareClicked(object sender, RoutedEventArgs e)
        {
            List<StorageFile> files = new List<StorageFile>();
            foreach (var file in Playlist.Items)
            {
                if (await NativeMethods.GetStorageFileFromPathAsync(file.Path) is StorageFile f)
                {
                    files.Add(f);
                }
            }
            if (files.Count == 0)
                return;
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            shareService.Title = Playlist.Title;
            shareService.Description = string.Empty;
            shareService.AddFiles(files);
            shareService.ShowShareUI();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Title))
            {
                Playlist = PlaylistManager.Instance.GetPlaylist(Title);
            }
            PlaylistManager.Instance.PlaylistChanged += OnPlaylistChanged;
            Bindings.Update();
        }

        private void OnPlaylistChanged(object sender, PlaylistChangedEventArgs e)
        {
            switch (e.Action)
            {
                case PlaylistChangeAction.Content:
                    if (e.NewTitle == Playlist.Title)
                    {
                        Playlist = e.Playlist;
                        Bindings.Update();
                    }
                    return;
                case PlaylistChangeAction.Rename:
                    if (e.OldTitle == Playlist.Title ||
                        e.NewTitle == Playlist.Title)
                    {
                        Playlist = e.Playlist;
                        Bindings.Update();
                    }
                    break;
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            PlaylistManager.Instance.PlaylistChanged -= OnPlaylistChanged;
        }

        private async void OnMenuRenameClicked(object sender, RoutedEventArgs e)
        {
            var newName = await FieldEditor.ShowAsync(
                Playlist.Title,
                CommonSharedStrings.RenameString,
                CommonSharedStrings.NewNameEmptyPrompt,
                "[^ ]");
            if (newName == null)
                return;
            try
            {
                await PlaylistManager.Instance.RenameAsync(Playlist.Title, newName);
            }
            catch (ArgumentException ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.InternalErrorTitle);
                await dialog.ShowAsync();
            }
        }
        private async void MessagePrompt(string content, string title = null)
        {
            if (title == null)
                await new MessageDialog(content).ShowAsync();
            else
                await new MessageDialog(content, title).ShowAsync();
        }

        private async void OnExportM3uClicked(object sender, RoutedEventArgs e)
        {
            try
            {
                FileSavePicker picker = new FileSavePicker()
                {
                    SuggestedFileName = Playlist.Title,
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
                    sw.Write(M3u.Export(Playlist.Items));
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
            try
            {
                var fileName = await FieldEditor.ShowAsync($"{Playlist.Title}.wpl",
                    CommonSharedStrings.ChooseFileName,
                    CommonSharedStrings.ValidFilenameRequired,
                    @"^(?!((con|prn|aux)((\.[^\\/:*?<>|" + "\"" + @"]{1,3}$)|$))|[\s\.])[^\\/:*?<>|" + "\"" + @"]{1,254}$");
                if (fileName == null)
                    return;
                var picker = new FolderPicker();
                picker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
                picker.ViewMode = PickerViewMode.List;
                picker.FileTypeFilter.Add(".wpl");
                var folder = await picker.PickSingleFolderAsync();
                if (folder == null)
                    return;
                await PlaylistManager.Instance.ExportAsync(Playlist, folder, fileName);
                MessagePrompt(CommonSharedStrings.PlaylistSaved);
            }
            catch (Exception ex)
            {
                MessagePrompt(ex.Message, CommonSharedStrings.FailedToSavePlaylist);
            }
        }
    }
}
