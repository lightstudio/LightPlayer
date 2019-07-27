using System;
using Light.Flyout;
using Light.Model;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Light.Shell;
using Windows.ApplicationModel.DataTransfer;
using Light.Utilities;
using Light.Managed.Database.Entities;
using System.Collections.Generic;
using Light.View.Library.Detailed;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using System.Linq;

namespace Light.Controls
{
    public sealed partial class LibraryTileControl : UserControl
    {
        private readonly UIViewSettings _currentViewSettings;

        #region DependencyProperty Declaration
        public static readonly DependencyProperty ItemTypedProperty =
            DependencyProperty.Register("ItemType",
                typeof(CommonItemType),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(CommonItemType)));

        public static readonly DependencyProperty InternalDbEntityIdProperty =
            DependencyProperty.Register("InternalDbEntityId",
                typeof(int),
                typeof(LibraryTileControl),
                new PropertyMetadata(-1));

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle",
                typeof(string),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ItemTypeProperty =
            DependencyProperty.Register("ItemType",
                typeof(CommonItemType),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(CommonItemType)));

        public static readonly DependencyProperty ExtendedArtistNameProperty =
            DependencyProperty.Register("ExtendedArtistName",
                typeof(string),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(string)));

        public static readonly DependencyProperty ExtendedFilePathProperty =
            DependencyProperty.Register("ExtendedFilePath",
                typeof(string),
                typeof(LibraryTileControl),
                new PropertyMetadata(default(string)));
        #endregion

        public int InternalDbEntityId
        {
            get
            {
                return (int)GetValue(InternalDbEntityIdProperty);
            }
            set
            {
                SetValue(InternalDbEntityIdProperty, value);
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
        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        public LibraryTileControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            _currentViewSettings = UIViewSettings.GetForCurrentView();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CommonViewItemModel)
            {
                var ctx = (CommonViewItemModel)DataContext;

                Title = ctx.Title;
                Subtitle = ctx.Content;
                ItemType = ctx.Type;
                InternalDbEntityId = ctx.InternalDbEntityId;
                ExtendedArtistName = ctx.ExtendedArtistName;
                ExtendedFilePath = ctx.ExtendedFilePath;

                CheckAndUpdateImageProperty();
            }
        }

        private void OnItemPointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != Windows.Devices.Input.PointerDeviceType.Touch)
            {
                MouseOverlayMenu.Visibility = Visibility.Visible;
            }
        }
        private void OnItemPointerExited(object sender, PointerRoutedEventArgs e)
        {
            MouseOverlayMenu.Visibility = Visibility.Collapsed;
        }
        private void OnPointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            MouseOverlayMenu.Visibility = Visibility.Collapsed;
        }

        private void CheckAndUpdateImageProperty()
        {
            if (DataContext is CommonViewItemModel)
            {
                var ctx = (CommonViewItemModel)DataContext;
                if (ctx.Type == CommonItemType.Album)
                {
                    CoverImage.ThumbnailTag = new ThumbnailTag
                    {
                        Fallback = "Album,AlbumPlaceholder",
                        ArtistName = ctx.ExtendedArtistName,
                        AlbumName = ctx.Title,
                        ThumbnailPath = ctx.ExtendedFilePath,
                    };
                }
                else if (ctx.Type == CommonItemType.Artist)
                {
                    CoverImage.ThumbnailTag = new ThumbnailTag
                    {
                        ArtistName = ctx.ExtendedArtistName,
                        Fallback = "Artist,ArtistPlaceholder"
                    };
                }
            }
        }
        private async void OnShareButtonClicked(object sender, RoutedEventArgs e)
        {
            await ((CommonViewItemModel)DataContext).ShareAsync();
        }

        private async void OnAlbumMenuAutoMatchClicked(object sender, RoutedEventArgs e)
        {
            var context = (CommonViewItemModel)DataContext;
            var searchView = new ThumbnailSearchFlyout(context);
            await searchView.ShowAsync();
        }

        private void OnItemTapped(object sender, TappedRoutedEventArgs e)
        {
            MouseOverlayMenu.Visibility = Visibility.Collapsed;
            if (e.OriginalSource == PlayButton ||
                e.OriginalSource == AddButton ||
                e.OriginalSource == ShareButton)
                return;
            var data = DataContext as CommonViewItemModel;
            Type destPageType = null;

            switch (data.Type)
            {
                case CommonItemType.Album:
                    destPageType = typeof(AlbumDetailView);
                    break;
                case CommonItemType.Artist:
                    destPageType = typeof(ArtistDetailView);
                    break;
            }

            if (destPageType != null)
            {
                Messenger.Default.Send(
                    new GenericMessage<Tuple<Type, int>>(
                        new Tuple<Type, int>(destPageType, data.InternalDbEntityId)),
                            CommonSharedStrings.FrameViewNavigationIntMessageToken);
            }
        }

        private void OnDragStarting(UIElement sender, DragStartingEventArgs args)
        {
            IEnumerable<DbMediaFile> files = null;
            var _vm = DataContext as CommonViewItemModel;
            switch (_vm.Type)
            {
                case CommonItemType.Album:
                    files = _vm.InternalDbEntityId.GetAlbumById()
                        .MediaFiles
                        .OrderBy(c => c.DiscNumber)
                        .ThenBy(c => c.TrackNumber);
                    break;
                case CommonItemType.Artist:
                    files = _vm.InternalDbEntityId.GetArtistById()
                        .MediaFiles
                        .OrderBy(c => c.Album)
                        .ThenBy(c => c.DiscNumber)
                        .ThenBy(c => c.TrackNumber);
                    break;
            }
            if (files != null)
            {
                args.DragUI.SetContentFromDataPackage();
                args.Data.RequestedOperation = DataPackageOperation.Copy;
                args.Data.SetText(DragHelper.Add(files));
            }
        }

        private void OnDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {
            DragHelper.Remove(((CommonViewItemModel)DataContext).File);
        }
    }
}
