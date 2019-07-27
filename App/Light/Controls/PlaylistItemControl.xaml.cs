using Light.Core;
using Light.Managed.Database.Native;
using Light.Shell;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Light.Controls
{
    public sealed partial class PlaylistItemControl : UserControl
    {
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(PlaylistItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle",
                typeof(string),
                typeof(PlaylistItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty PlaylistItemProperty =
            DependencyProperty.Register("PlaylistItem",
                typeof(PlaylistItem),
                typeof(PlaylistItemControl),
                new PropertyMetadata(default(PlaylistItem)));

        public static readonly DependencyProperty PlaylistProperty =
            DependencyProperty.Register("Playlist",
                typeof(Playlist),
                typeof(PlaylistItemControl),
                new PropertyMetadata(default(Playlist)));

        public static readonly DependencyProperty IsControlDisplayedProperty =
            DependencyProperty.Register("IsControlDisplayed",
                typeof(bool),
                typeof(PlaylistItemControl),
                new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty DisableTapToPlayProperty =
            DependencyProperty.Register(nameof(DisableTapToPlay),
                typeof(bool),
                typeof(PlaylistItemControl),
                new PropertyMetadata(false));

        public PlaylistItem PlaylistItem
        {
            get
            {
                return (PlaylistItem)GetValue(PlaylistItemProperty);
            }
            set
            {
                SetValue(PlaylistItemProperty, value);
            }
        }
        public Playlist Playlist
        {
            get
            {
                return (Playlist)GetValue(PlaylistProperty);
            }
            set
            {
                SetValue(PlaylistProperty, value);
            }
        }
        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }
        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }
        public bool IsControlDisplayed
        {
            get { return (bool)GetValue(IsControlDisplayedProperty); }
            set { SetValue(IsControlDisplayedProperty, value); }
        }

        public bool DisableTapToPlay
        {
            get { return (bool)GetValue(DisableTapToPlayProperty); }
            set { SetValue(DisableTapToPlayProperty, value); }
        }

        public PlaylistItemControl()
        {
            this.InitializeComponent();
        }
        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                IsControlDisplayed = true;
            }
        }
        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            IsControlDisplayed = false;
        }
        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            IsControlDisplayed = false;
        }

        private async Task PlayCurrentTrack()
        {
            var list = Playlist.Items.ToList();
            await Core.PlaybackControl.Instance.AddAndSetIndexAt(
                list.Select(item => MusicPlaybackItem.CreateFromMediaFile(item.ToMediaFile())),
                list.IndexOf(PlaylistItem));
        }

        private async void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (DisableTapToPlay)
            {
                return;
            }
            await PlayCurrentTrack();
        }
        private async void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse)
            {
                IsControlDisplayed = true;
            }
            else
            {
                if (DisableTapToPlay)
                {
                    return;
                }
                await PlayCurrentTrack();
            }
        }
        private async void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            await PlayCurrentTrack();
        }
        private void OnDeleteMenuClicked(object sender, RoutedEventArgs e)
        {
            Playlist.Items.Remove(PlaylistItem);
        }
        private async void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(
                    PlaylistItem.ToMediaFile()));
        }
        private async void OnShareMenuClicked(object sender, RoutedEventArgs e)
        {
            var f = await NativeMethods.GetStorageFileFromPathAsync(PlaylistItem.Path) as StorageFile;
            if (f == null)
                return;
            var shareService = ShareServices.GetForCurrentView();
            shareService.PrecleanForSession();
            shareService.Title = Title;
            shareService.Description = string.Empty;
            shareService.AddFile(f);
            shareService.ShowShareUI();
        }

        private async void OnAddNextClick(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(
                    PlaylistItem.ToMediaFile()), -2);
        }

        private async void OnOpenFileLocationClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var folderPath = Path.GetDirectoryName(PlaylistItem.Path);
                var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);

                if (folder != null)
                {
                    await Launcher.LaunchFolderAsync(folder,
                        new FolderLauncherOptions
                        {
                            ItemsToSelect =
                            {
                                await StorageFile.GetFileFromPathAsync(PlaylistItem.Path)
                            }
                        });
                }
            }
            catch
            {

            }
        }

        private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            args.Data.SetText(DragHelper.Add(PlaylistItem));
        }

        private void OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            DragHelper.Remove(PlaylistItem);
        }
    }
}
