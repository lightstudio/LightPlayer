using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Entities;
using Light.Managed.Database.Native;
using Light.Model;
using Light.Utilities;
using Light.View.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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
    public sealed partial class MusicListItemControl : UserControl
    {
        public MusicListItemControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ArtistProperty =
            DependencyProperty.Register(
                nameof(Artist),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty FileListProperty =
            DependencyProperty.Register(
                nameof(FileList),
                typeof(IEnumerable<CommonViewItemModel>),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(IEnumerable<CommonViewItemModel>)));

        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register(
                nameof(File),
                typeof(CommonViewItemModel),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(CommonViewItemModel)));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty AlbumProperty =
            DependencyProperty.Register(
                nameof(Album),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty YearProperty =
            DependencyProperty.Register(
                nameof(Year),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty GenreProperty =
            DependencyProperty.Register(
                nameof(Genre),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));


        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register(
                nameof(Index),
                typeof(string),
                typeof(MusicListItemControl),
                new PropertyMetadata(default(string)));


        public string Index
        {
            get { return (string)GetValue(IndexProperty); }
            set { SetValue(IndexProperty, value); }
        }

        public string Artist
        {
            get { return (string)GetValue(ArtistProperty); }
            set { SetValue(ArtistProperty, value); }
        }

        public string Album
        {
            get { return (string)GetValue(AlbumProperty); }
            set { SetValue(AlbumProperty, value); }
        }

        public string Year
        {
            get { return (string)GetValue(YearProperty); }
            set { SetValue(YearProperty, value); }
        }

        public string Genre
        {
            get { return (string)GetValue(GenreProperty); }
            set { SetValue(GenreProperty, value); }
        }

        public IEnumerable<CommonViewItemModel> FileList
        {
            get { return (IEnumerable<CommonViewItemModel>)GetValue(FileListProperty); }
            set { SetValue(FileListProperty, value); }
        }

        public CommonViewItemModel File
        {
            get { return (CommonViewItemModel)GetValue(FileProperty); }
            set { SetValue(FileProperty, value); }
        }

        public string Duration
        {
            get { return (string)GetValue(DurationProperty); }
            set { SetValue(DurationProperty, value); }
        }

        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        private async void PlayCurrent()
        {
            var list = FileList.ToList();
            await Core.PlaybackControl.Instance.AddAndSetIndexAt(
                list.Select(item => MusicPlaybackItem.CreateFromMediaFile(item.File)),
            list.IndexOf(File));
        }

        private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonPanel2.Visibility = Visibility.Collapsed;
            PlayCurrent();
        }
        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse)
            {
                ButtonPanel.Visibility = Visibility.Visible;
                ButtonPanel2.Visibility = Visibility.Visible;
            }
            else
            {
                PlayCurrent();
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonPanel2.Visibility = Visibility.Collapsed;
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                ButtonPanel.Visibility = Visibility.Visible;
                ButtonPanel2.Visibility = Visibility.Visible;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            ButtonPanel2.Visibility = Visibility.Collapsed;
        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            PlayCurrent();
        }

        private async void OnAddClicked(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(File.File));
        }

        private async void OnAddAsNextClicked(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(File.File), -2);
        }

        private async void OnOpenFileClicked(object sender, RoutedEventArgs e)
        {
            var file = await NativeMethods.GetStorageFileFromPathAsync(File.File.Path);
            var item = file as IStorageItem2;
            if (item == null)
                return;
            var parent = await item.GetParentAsync();
            if (parent != null)
            {
                await Launcher.LaunchFolderAsync(parent,
                    new FolderLauncherOptions
                    {
                        ItemsToSelect = { file }
                    });
            }
        }

        private async void OnShowPropertiesClicked(object sender, RoutedEventArgs e)
        {
            await MediaFilePropertiesDialog.ShowFilePropertiesViewAsync(File.File.Id);
        }

        private void OnAddToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            PlaylistPicker.Pick(PlaylistItem.FromMediaFile(File.File));
        }

        private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            args.Data.SetText(DragHelper.Add(File.File));
        }

        private void OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            DragHelper.Remove(File.File);
        }
    }
}
