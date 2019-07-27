using Light.Core;
using Light.Flyout;
using Light.Managed.Database;
using Light.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Light.Controls
{
    public sealed partial class PhoneListItemControl : UserControl
    {
        public string Title
        {
            get { return (string)GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PhoneListItemControl), new PropertyMetadata(string.Empty));

        public string Subtitle
        {
            get { return (string)GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(PhoneListItemControl), new PropertyMetadata(string.Empty));

        public event EventHandler<TappedRoutedEventArgs> InfoPanelTapped;

        internal MediaThumbnail ThumbnailControl => CoverImage;

        public PhoneListItemControl()
        {
            this.InitializeComponent();
        }

        private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext is CommonViewItemModel)
            {
                var ctx = (CommonViewItemModel)DataContext;

                if (ctx.Type == CommonItemType.Album)
                {
                    Title = ctx.Title;
                    Subtitle = ctx.ExtendedArtistName;
                    SubtitleText.Visibility = Visibility.Visible;
                }
                else if (ctx.Type == CommonItemType.Artist)
                {
                    Title = ctx.Title;
                    Subtitle = ctx.Content;
                    SubtitleText.Visibility = Visibility.Visible;
                }

                CheckAndUpdateImageProperty();
            }
        }

        private void CheckAndUpdateImageProperty()
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

        private void Play()
        {
            var ctx = (CommonViewItemModel)DataContext;
            if (ctx.Type == CommonItemType.Album)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var album = context.Albums
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId);
                    album.Play(requireClear: true, isInsert: false);
                }
            }
            else if (ctx.Type == CommonItemType.Artist)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var artist = context.Artists
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId);
                    artist.Play(requireClear: true, isInsert: false);
                }
            }
        }

        private void OnPlayClicked(object sender, RoutedEventArgs e)
        {
            Play();
        }

        private void OnAddClicked(object sender, RoutedEventArgs e)
        {
            var ctx = (CommonViewItemModel)DataContext;
            if (ctx.Type == CommonItemType.Album)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var album = context.Albums
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId);
                    album.Play(requireClear: false, isInsert: false);
                }
            }
            else if (ctx.Type == CommonItemType.Artist)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    var artist = context.Artists
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId);
                    artist.Play(requireClear: false, isInsert: false);
                }
            }
        }

        private void OnAddToPlaylistClicked(object sender, RoutedEventArgs e)
        {
            IEnumerable<PlaylistItem> items;
            var ctx = (CommonViewItemModel)DataContext;
            if (ctx.Type == CommonItemType.Album)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    items = context.Albums
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId)
                        .MediaFiles.Select(x => PlaylistItem.FromMediaFile(x)).ToArray();
                }
            }
            else if (ctx.Type == CommonItemType.Artist)
            {
                using (var scope = ApplicationServiceBase.App.GetScope())
                using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
                {
                    items = context.Artists
                        .Include(c => c.MediaFiles)
                        .First(i => i.Id == ctx.InternalDbEntityId)
                        .MediaFiles.Select(x => PlaylistItem.FromMediaFile(x)).ToArray();
                }
            }
            else
            {
                throw new NotSupportedException();
            }
            PlaylistPicker.Pick(items);
        }

        private void OnCoverImageTapped(object sender, TappedRoutedEventArgs e)
        {
            Play();
        }

        private void OnInfoPanelTapped(object sender, TappedRoutedEventArgs e)
        {
            InfoPanelTapped?.Invoke(this, e);
        }

        private async void OnAlbumMenuAutoMatchClicked(object sender, RoutedEventArgs e)
        {
            var context = (CommonViewItemModel)DataContext;
            var searchView = new ThumbnailSearchFlyout(context);
            await searchView.ShowAsync();
        }
    }
}
