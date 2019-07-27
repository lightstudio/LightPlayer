using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Light.Common;
using Light.Managed.Online;
using Light.Model;
using Light.Utilities;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Media.Imaging;
using Light.Utilities.UserInterfaceExtensions;
using Light.Core;
using WinRTXamlToolkit.AwaitableUI;
using System.Collections.Generic;
using System.Diagnostics;
using Windows.UI.Core;

namespace Light.Controls
{
    public class MediaThumbnailImageChangedEventArgs : EventArgs
    {
        public bool IsDefaultImage { get; set; }
    }

    public sealed class MediaThumbnail : Control
    {
        public static readonly DependencyProperty StretchProperty =
            DependencyProperty.Register(
                nameof(Stretch), typeof(Stretch), typeof(MediaThumbnail),
                new PropertyMetadata(Stretch.Uniform));

        public static readonly DependencyProperty ThumbnailTagProperty =
            DependencyProperty.Register(
                nameof(ThumbnailTag), typeof(ThumbnailTag), typeof(MediaThumbnail),
                new PropertyMetadata(default(ThumbnailTag), OnImagePropertyChanged));

        static Lazy<BitmapImage> DefaultArtistImage =
            new Lazy<BitmapImage>(() => new BitmapImage(
                new Uri(CommonSharedStrings.DefaultArtistImagePath)));

        static Lazy<BitmapImage> DefaultAlbumImage =
            new Lazy<BitmapImage>(() => new BitmapImage(
                new Uri(CommonSharedStrings.DefaultAlbumImagePath)));

        static Lazy<BitmapImage> EmptyBitmap = new Lazy<BitmapImage>();
        static Lazy<BitmapImage> DefaultLargeArtistImage = new Lazy<BitmapImage>(() => new BitmapImage(new Uri("ms-appx:///Assets/Guitar Player.jpg")));
        static Lazy<BitmapImage> DefaultLargeAlbumImage = new Lazy<BitmapImage>(() => new BitmapImage(new Uri("ms-appx:///Assets/IntroImage.jpg")));

        public static readonly DependencyProperty EnableAnimationProperty =
            DependencyProperty.Register("EnableAnimation", typeof(bool), typeof(MediaThumbnail), new PropertyMetadata(false));

        public event EventHandler<MediaThumbnailImageChangedEventArgs> ImageChanged;

        AsyncLock l = new AsyncLock();
        bool _delayLoad = false;
        ThumbnailTag _tag;

        List<string> _fallbackStates;
        int _currentFallbackStage = -1;

        Image Thumbnail;
        Storyboard FadeIn;
        Storyboard FadeOut;

        private bool _albumChangedHandlerAdded;
        private bool _artistChangedHandlerAdded;

        public Stretch Stretch
        {
            get { return (Stretch)GetValue(StretchProperty); }
            set { SetValue(StretchProperty, value); }
        }

        public ThumbnailTag ThumbnailTag
        {
            get { return (ThumbnailTag)GetValue(ThumbnailTagProperty); }
            set { SetValue(ThumbnailTagProperty, value); }
        }

        public MediaThumbnail()
        {
            this.DefaultStyleKey = typeof(MediaThumbnail);
        }
        public bool EnableAnimation
        {
            get { return (bool)GetValue(EnableAnimationProperty); }
            set { SetValue(EnableAnimationProperty, value); }
        }

        private void CleanStates()
        {
            if (_fallbackStates == null)
            {
                return;
            }
            // Clear event handlers when switching tags.
            foreach (var state in _fallbackStates)
            {
                switch (state)
                {
                    case "album":
                        if (_albumChangedHandlerAdded)
                        {
                            ThumbnailManager.RemoveHandler(_tag.ArtistName, _tag.AlbumName, OnAlbumImageChanged);
                            _albumChangedHandlerAdded = false;
                        }
                        break;
                    case "artist":
                        if (_artistChangedHandlerAdded)
                        {
                            ThumbnailManager.RemoveHandler(_tag.ArtistName, OnArtistImageChanged);
                            _artistChangedHandlerAdded = false;
                        }
                        break;
                }
            }
            _currentFallbackStage = -1;
        }

        private async void OnArtistImageChanged(string artistName, bool hasImage)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => OnArtistImageChanged(artistName, hasImage));
                return;
            }

            var pos = _fallbackStates.IndexOf("artist");
            Debug.Assert(pos != -1);

            if (_currentFallbackStage == pos)
            {
                if (!hasImage)
                {
                    await FallbackMoveNext();
                }
                else
                {
                    await LoadArtistImage();
                }
            }
            else if (_currentFallbackStage > pos && hasImage)
            {
                _currentFallbackStage = pos;
                await LoadArtistImage();
            }
        }

        private async void OnAlbumImageChanged(string artistName, string albumName, bool hasImage)
        {
            if (!Dispatcher.HasThreadAccess)
            {
                await Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () => OnAlbumImageChanged(artistName, albumName, hasImage));
                return;
            }

            var pos = _fallbackStates.IndexOf("album");
            Debug.Assert(pos != -1);

            if (_currentFallbackStage == pos)
            {
                if (!hasImage)
                {
                    await FallbackMoveNext();
                }
                else
                {
                    await LoadAlbumImage();
                }
            }
            else if (_currentFallbackStage > pos && hasImage)
            {
                _currentFallbackStage = pos;
                await LoadAlbumImage();
            }
        }

        private async Task FallbackMoveNext()
        {
            if (_currentFallbackStage < _fallbackStates.Count - 1)
            {
                _currentFallbackStage++;
                var state = _fallbackStates[_currentFallbackStage];
                switch (state)
                {
                    case "album":
                        await LoadAlbumImage();
                        break;
                    case "artist":
                        await LoadArtistImage();
                        break;
                    case "albumplaceholder":
                        LoadAlbumPlaceholder();
                        break;
                    case "artistplaceholder":
                        LoadArtistPlaceholder();
                        break;
                    case "defaultartistlarge":
                        LoadDefaultArtistLarge();
                        break;
                }
            }
        }

        private void LoadLastFallback()
        {
            var state = _fallbackStates.Last();
            switch (state)
            {
                case "albumplaceholder":
                    LoadAlbumPlaceholder();
                    break;
                case "artistplaceholder":
                    LoadArtistPlaceholder();
                    break;
                case "defaultartistlarge":
                    LoadDefaultArtistLarge();
                    break;
            }
        }

        private async Task AnimatedSetThumbnailSource(BitmapImage image, bool hideBackground = true)
        {
            if (EnableAnimation)
            {
                await FadeOut.BeginAsync();
            }

            Thumbnail.Source = image;
            if (EnableAnimation)
            {
                await FadeIn.BeginAsync();
            }
            if (hideBackground)
            {
                Background = null;
            }
            ImageChanged?.Invoke(this, new MediaThumbnailImageChangedEventArgs { IsDefaultImage = !hideBackground });
        }

        private async Task LoadAlbumImage()
        {
            if (!_albumChangedHandlerAdded)
            {
                ThumbnailManager.OnAlbumImageChanged(_tag.ArtistName, _tag.AlbumName, OnAlbumImageChanged);
                _albumChangedHandlerAdded = true;
            }

            var (image, filePresent) = await ThumbnailManager.GetAsync(
                _tag.ArtistName,
                _tag.AlbumName,
                (int)Math.Max(
                    Math.Ceiling(ActualWidth),
                    Math.Ceiling(ActualHeight)));
            if (filePresent)
            {
                if (image == null)
                {
                    await FallbackMoveNext();
                }
                else
                {
                    await AnimatedSetThumbnailSource(image);
                }
            }
            else
            {
                //LoadLastFallback();
                ThumbnailAgent.Fetch(_tag.ArtistName, _tag.AlbumName, _tag.ThumbnailPath);
            }
        }

        private async Task LoadArtistImage()
        {
            if (!_artistChangedHandlerAdded)
            {
                ThumbnailManager.OnArtistImageChanged(_tag.ArtistName, OnArtistImageChanged);
                _artistChangedHandlerAdded = true;
            }

            var (image, filePresent) = await ThumbnailManager.GetAsync(
                _tag.ArtistName,
                (int)Math.Max(
                    Math.Ceiling(ActualWidth),
                    Math.Ceiling(ActualHeight)));
            if (filePresent)
            {
                if (image == null)
                {
                    await FallbackMoveNext();
                }
                else
                {
                    await AnimatedSetThumbnailSource(image);
                }
            }
            else
            {
                //LoadLastFallback();
                ThumbnailAgent.Fetch(_tag.ArtistName);
            }
        }

        private async void LoadAlbumPlaceholder()
        {
            Background = new ImageBrush
            {
                Stretch = Stretch,
                ImageSource = DefaultAlbumImage.Value
            };
            if (Thumbnail != null)
            {
                await AnimatedSetThumbnailSource(EmptyBitmap.Value, false);
            }
        }

        private async void LoadArtistPlaceholder()
        {
            Background = new ImageBrush
            {
                Stretch = Stretch,
                ImageSource = DefaultArtistImage.Value
            };
            if (Thumbnail != null)
            {
                await AnimatedSetThumbnailSource(EmptyBitmap.Value, false);
            }
        }

        private async void LoadDefaultArtistLarge()
        {
            Background = new ImageBrush
            {
                Stretch = Stretch,
                ImageSource = DefaultLargeArtistImage.Value
            };
            if (Thumbnail != null)
            {
                await AnimatedSetThumbnailSource(EmptyBitmap.Value, false);
            }
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            Thumbnail = (Image)GetTemplateChild(nameof(Thumbnail));
            if (EnableAnimation)
            {
                FadeIn = (Storyboard)GetTemplateChild(nameof(FadeIn));
                FadeOut = (Storyboard)GetTemplateChild(nameof(FadeOut));
            }
            Unloaded += OnUnloaded;
            Loaded += OnLoaded;
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_delayLoad && _currentFallbackStage == -1)
            {
                LoadLastFallback();
                await FallbackMoveNext();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CleanStates();
        }

        private static async void OnImagePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var sender = (MediaThumbnail)d;
            var value = (ThumbnailTag)e.NewValue;

            if (sender == null || value == null) return;
            using (await sender.l.LockAsync())
            {
                // Check if we can reuse last loaded images, which can sometimes avoid unnecessary "blinking".
                if (sender._tag != null)
                {
                    var oldTag = sender._tag;
                    if (oldTag.AlbumName == value.AlbumName &&
                        oldTag.ArtistName == value.ArtistName &&
                        //oldTag.ThumbnailPath == value.ThumbnailPath &&
                        oldTag.Fallback == value.Fallback)
                    {
                        return;
                    }
                }
                sender.CleanStates();
                sender._fallbackStates = new List<string>(
                    value.Fallback
                    .Split(',')
                    .Select(x => x.Trim().ToLower()));
                sender._tag = value;
                if (sender.Thumbnail != null)
                {
                    sender.LoadLastFallback();
                    await sender.FallbackMoveNext();
                }
                else
                {
                    sender._delayLoad = true;
                }
            }
        }
    }
}
