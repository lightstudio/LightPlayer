using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Entities;
using Light.Managed.Database.Native;
using Light.Model;
using Light.Shell;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.Input;
using Windows.Storage;
using Windows.System;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Light.Controls
{
    public sealed partial class LibraryListItemControl : UserControl
    {
        private readonly UIViewSettings _currentViewSettings;

        #region DependencyProperty Declaration
        public static readonly DependencyProperty ItemTypedProperty =
            DependencyProperty.Register("ItemType",
                typeof(CommonItemType),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(CommonItemType)));

        public static readonly DependencyProperty FileListProperty =
            DependencyProperty.Register("FileList",
                typeof(IEnumerable<CommonViewItemModel>),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(IEnumerable<DbMediaFile>)));

        public static readonly DependencyProperty FileProperty =
            DependencyProperty.Register("File",
                typeof(CommonViewItemModel),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(DbMediaFile)));
        
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle",
                typeof(string),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ItemTypeProperty =
            DependencyProperty.Register("ItemType",
                typeof(CommonItemType),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(CommonItemType)));

        public static readonly DependencyProperty ImageHashProperty =
            DependencyProperty.Register("ImageHash",
                typeof(string),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ExtendedArtistNameProperty =
            DependencyProperty.Register("ExtendedArtistName",
                typeof(string),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ExtendedFilePathProperty =
            DependencyProperty.Register("ExtendedFilePath",
                typeof(string),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(string)));
        public static readonly DependencyProperty IsControlDisplayedProperty =
            DependencyProperty.Register("IsControlDisplayed",
                typeof(bool),
                typeof(LibraryListItemControl),
                new PropertyMetadata(default(bool)));
        #endregion

        public IEnumerable<CommonViewItemModel> FileList
        {
            get
            {
                return (IEnumerable<CommonViewItemModel>)GetValue(FileListProperty);
            }
            set
            {
                SetValue(FileListProperty, value);
            }
        }
        public CommonViewItemModel File
        {
            get
            {
                return (CommonViewItemModel)GetValue(FileProperty);
            }
            set
            {
                SetValue(FileProperty, value);
            }
        }
        public CommonItemType ItemType
        {
            get
            {
                return (CommonItemType)GetValue(ItemTypeProperty);
            }
            set
            {
                SetValue(ItemTypeProperty, value);
            }
        }
        public string ExtendedFilePath
        {
            get
            {
                return (string)GetValue(ExtendedFilePathProperty);
            }
            set
            {
                SetValue(ExtendedFilePathProperty, value);
            }
        }
        public string ExtendedArtistName
        {
            get
            {
                return (string)GetValue(ExtendedArtistNameProperty);
            }
            set
            {
                SetValue(ExtendedArtistNameProperty, value);
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
        public string ImageHash
        {
            get
            {
                return (string)this.GetValue(ImageHashProperty);
            }
            set
            {
                this.SetValue(ImageHashProperty, value);
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

        public LibraryListItemControl()
        {
            this.InitializeComponent();
            _currentViewSettings = UIViewSettings.GetForCurrentView();
        }

        private async void PlayCurrent()
        {
            var list = FileList.ToList();
            var idx = list.IndexOf(File);
            if (idx == -1)
            {
                return;
            }
            Core.PlaybackControl.Instance.Clear();
            await Core.PlaybackControl.Instance.AddFile(
                from item
                in list
                select MusicPlaybackItem.CreateFromMediaFile(item.File));
            Core.PlaybackControl.Instance.SetIndex(idx);
        }

        private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            IsControlDisplayed = false;
            PlayCurrent();
        }
        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            if (e.PointerDeviceType == PointerDeviceType.Mouse)
            {
                IsControlDisplayed = true;
            }
            else
            {
                PlayCurrent();
            }
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
        private async void OnShareMenuClicked(object sender, RoutedEventArgs e)
        {
            await ((CommonViewItemModel)DataContext).ShareAsync();
        }
        private void OnDeleteMenuClicked(object sender, RoutedEventArgs e)
        {

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
