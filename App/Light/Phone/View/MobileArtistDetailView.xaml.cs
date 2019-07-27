using ColorThiefDotNet;
using Light.Core;
using Light.Flyout;
using Light.Managed.Database;
using Light.Managed.Database.Entities;
using Light.ViewModel.Library.Detailed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Phone.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MobileArtistDetailView : MobileBasePage
    {
        public override bool ReserveSpaceForStatusBar => false;
        private ArtistDetailViewModel ViewModel => (ArtistDetailViewModel)DataContext;
        private int _id;
        private bool _animatedIn = false;

        public MobileArtistDetailView()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter != null)
            {
                _id = (int)e.Parameter;
                DataContext = new ArtistDetailViewModel((int)e.Parameter, Frame);
            }
            await ViewModel.LoadTitleAsync();
            ConnectedAnimation imageAnimation = ConnectedAnimationService.GetForCurrentView().GetAnimation("image");
            if (imageAnimation != null)
            {
                _animatedIn = imageAnimation.TryStart(Thumbnail);
            }
            await ViewModel.LoadContentAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.Cleanup();
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            if (_animatedIn && 
                (e.SourcePageType == typeof(LibrarySongsView) ||
                e.SourcePageType == typeof(MobileSearchView)))
            {
                ConnectedAnimationService.GetForCurrentView().PrepareToAnimate(
                    "image",
                    Thumbnail);
            }
            base.OnNavigatingFrom(e);
        }

        private void OnAddIconClick(object sender, RoutedEventArgs e)
        {
            var album = (sender as Button).DataContext as DbAlbum;
            album.Play(false, false);
        }

        private void OnPlayIconClick(object sender, RoutedEventArgs e)
        {
            var album = (sender as Button).DataContext as DbAlbum;
            album.Play(true, false);
        }

        private void OnAddToPlaylistClick(object sender, RoutedEventArgs e)
        {
            PlaylistPicker.Pick(
                ViewModel.ArtistAlbums
                .SelectMany(x => x.MediaFiles)
                .Select(x => PlaylistItem.FromMediaFile(x)));
        }

        private void OnShuffleAllTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            _id.GetArtistById().Shuffle();
        }

        private async void OnPlayButtonClick(object sender, RoutedEventArgs e)
        {
            var files = ViewModel.ArtistAlbums
                .SelectMany(x => x.MediaFiles)
                .Select(x => MusicPlaybackItem.CreateFromMediaFile(x))
                .ToArray();

            PlaybackControl.Instance.Clear();
            await PlaybackControl.Instance.AddFile(files, -1);
        }

        private void OnAddToNowPlayingClick(object sender, RoutedEventArgs e)
        {
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var artist = context.Artists
                    .Include(c => c.MediaFiles)
                    .First(i => i.Id == _id);
                artist.Play(requireClear: false, isInsert: false);
            }
        }

        private void OnAddButtonClick(object sender, RoutedEventArgs e)
        {
            var s = sender as FrameworkElement;
            var flyout = (MenuFlyout)FlyoutBase.GetAttachedFlyout(s);
            flyout.ShowAt(s, new Point(0, 0));
        }

        private async void UpdateBackgroundColor()
        {
            await Task.Delay(200);

            var target = new RenderTargetBitmap();
            try
            {
                await target.RenderAsync(Thumbnail);
            }
            catch
            {
                // workaround for RenderAsync throwing ArgumentOutOfRangeException 
                // when target control is not visible.
                return;
            }

            var isColorRetrievalApplicable = true;
            SolidColorBrush currentBrush = (SolidColorBrush)StatusBar.Background;
            SolidColorBrush candidateBrush = null;

            var pixels = await target.GetPixelsAsync();
            if (pixels.Length == 0)
            {
                isColorRetrievalApplicable = false;
            }
            else
            {
                var t = new ColorThief();
                var q = t.GetColor(pixels.ToArray());
                if (q == null)
                {
                    isColorRetrievalApplicable = false;
                }

                // If a candidate color can be retrieved, it will be retrieved using album reference.
                if (isColorRetrievalApplicable)
                {
                    byte a = q.Color.A,
                    r = (byte)(q.Color.R / 2),
                    g = (byte)(q.Color.G / 2),
                    b = (byte)(q.Color.B / 2);
                    var result = Windows.UI.Color.FromArgb(a, r, g, b);
                    candidateBrush = new SolidColorBrush(result);
                }
            }

            // Fallback color for no reference items
            if (!isColorRetrievalApplicable)
            {
                candidateBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 51, 51, 51));
            }

            // Will not update if we have the same color as previous one
            if (currentBrush?.Color != candidateBrush.Color)
            {
                // Animation first. Real value will be set later.
                var colorTransitionStoryBoard = (Storyboard)Resources["TransitionColorStoryboard"];
                ((ColorAnimation)colorTransitionStoryBoard.Children[0]).To = candidateBrush.Color;

                // Let it fire!
                colorTransitionStoryBoard.Begin();
            }
        }

        private void OnThumbnailImageChanged(object sender, EventArgs e)
        {
            UpdateBackgroundColor();
        }
    }
}
