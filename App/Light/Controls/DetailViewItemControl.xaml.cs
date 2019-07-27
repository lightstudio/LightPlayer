using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Entities;
using Light.Managed.Database.Native;
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
    public sealed partial class DetailViewItemControl : UserControl
    {
        public static readonly DependencyProperty ArtistProperty =
            DependencyProperty.Register(
                nameof(Artist),
                typeof(string),
                typeof(DetailViewItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty FileListProperty =
            DependencyProperty.Register(
                nameof(FileList),
                typeof(IEnumerable<DbMediaFile>),
                typeof(DetailViewItemControl),
                new PropertyMetadata(default(IEnumerable<DbMediaFile>)));

        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register(
                nameof(File),
                typeof(DbMediaFile),
                typeof(DetailViewItemControl),
                new PropertyMetadata(default(DbMediaFile)));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(DetailViewItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty DurationProperty =
            DependencyProperty.Register(
                nameof(Duration),
                typeof(string),
                typeof(DetailViewItemControl),
                new PropertyMetadata(default(string)));


        public static readonly DependencyProperty IndexProperty =
            DependencyProperty.Register(
                nameof(Index),
                typeof(string),
                typeof(DetailViewItemControl),
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

        public IEnumerable<DbMediaFile> FileList
        {
            get { return (IEnumerable<DbMediaFile>)GetValue(FileListProperty); }
            set { SetValue(FileListProperty, value); }
        }

        public DbMediaFile File
        {
            get { return (DbMediaFile)GetValue(FileProperty); }
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

        public DetailViewItemControl()
        {
            this.InitializeComponent();
        }

        private async void PlayCurrent()
        {
            var list = FileList.ToList();
            await Core.PlaybackControl.Instance.AddAndSetIndexAt(
                list.Select(item => MusicPlaybackItem.CreateFromMediaFile(item)),
            list.IndexOf(File));
        }

        private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
            PlayCurrent();
        }
        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse)
            {
                ButtonPanel.Visibility = Visibility.Visible;
            }
            else
            {
                PlayCurrent();
            }
        }

        private void OnPointerExited(object sender, PointerRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
        }

        private void OnPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                ButtonPanel.Visibility = Visibility.Visible;
            }
        }

        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            ButtonPanel.Visibility = Visibility.Collapsed;
        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            PlayCurrent();
        }

        private async void OnAddClicked(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(File));
        }

        private async void OnAddAsNextClicked(object sender, RoutedEventArgs e)
        {
            await Core.PlaybackControl.Instance.AddFile(
                MusicPlaybackItem.CreateFromMediaFile(File), -2);
        }

        private async void OnOpenFileClicked(object sender, RoutedEventArgs e)
        {
            var file = await NativeMethods.GetStorageFileFromPathAsync(File.Path);
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
            await MediaFilePropertiesDialog.ShowFilePropertiesViewAsync(File.Id);
        }

        private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            args.DragUI.SetContentFromDataPackage();
            args.Data.RequestedOperation = DataPackageOperation.Copy;
            args.Data.SetText(DragHelper.Add(File));
        }

        private void OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            DragHelper.Remove(File);
        }
    }
}
