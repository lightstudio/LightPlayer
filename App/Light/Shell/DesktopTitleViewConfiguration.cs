using System;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Light.Common;

namespace Light.Shell
{
    public class PageTitleBar : DependencyObject
    {
        public static readonly DependencyProperty TitleBarForegroundColorProperty = 
            DependencyProperty.Register("TitleBarForegroundColor", typeof(Color), typeof(PageTitleBar), 
                PropertyMetadata.Create(default(Color), OnTitleBarForegroundColorPropertyChanged));

        public static readonly DependencyProperty TitleBarBackgroundColorProperty =
            DependencyProperty.Register("TitleBarBackgroundColor", typeof(Color), typeof(PageTitleBar),
                PropertyMetadata.Create(default(Color), OnTitleBarBackgroundColorPropertyChanged));

        private static async void OnTitleBarBackgroundColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e != null)
            {
                var newValue = (Color) e.NewValue;
                switch (PlatformInfo.CurrentPlatform)
                {
                    case Platform.WindowsDesktop:
                        var appViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                        appViewTitleBar.InactiveBackgroundColor
                            = appViewTitleBar.ButtonInactiveBackgroundColor
                                = appViewTitleBar.ButtonBackgroundColor
                                    = appViewTitleBar.BackgroundColor
                                        = newValue;
                        break;
                    case Platform.WindowsMobile:
                        var statusBar = StatusBar.GetForCurrentView();
                        await statusBar.ShowAsync();
                        statusBar.BackgroundColor = newValue;
                        break;
                }
            }
        }

        private static async void OnTitleBarForegroundColorPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e != null)
            {
                var newValue = (Color) e.NewValue;
                
                switch (PlatformInfo.CurrentPlatform)
                {
                    case Platform.WindowsDesktop:
                        var appViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;
                        appViewTitleBar.ButtonForegroundColor
                            = appViewTitleBar.ForegroundColor
                                = newValue;
                        break;
                    case Platform.WindowsMobile:
                        var statusBar = StatusBar.GetForCurrentView();
                        await statusBar.ShowAsync();
                        statusBar.ForegroundColor = newValue;
                        break;
                }
            }
        }

        public Color TitleBarForegroundColor
        {
            get
            {
                return (Color) GetValue(TitleBarForegroundColorProperty);
            }
            set
            {
                SetValue(TitleBarForegroundColorProperty, value);
            }
        }

        public Color TitleBarBackgroundColor
        {
            get
            {
                return (Color)GetValue(TitleBarBackgroundColorProperty);
            }
            set
            {
                SetValue(TitleBarBackgroundColorProperty, value);
            }
        }
    }

    public static class DesktopTitleViewConfiguration
    {
        private static readonly Color ForegroundColorDarkTheme = Color.FromArgb(255, 255, 255, 255);

        private static readonly Color SplashScreenBackgroundColor = Color.FromArgb(255, 66, 67, 67);

        public static void EnterSplashScreen()
        {
            switch (PlatformInfo.CurrentPlatform)
            {
                case Platform.WindowsDesktop:
                    EnterDesktopView();
                    break;
                case Platform.WindowsMobile:
                    EnterMobileView();
                    break;
            }
        }

        private static void EnterDesktopView()
        {
            // Set Application Title View Color
            var appViewTitleBar = ApplicationView.GetForCurrentView().TitleBar;

            appViewTitleBar.InactiveBackgroundColor
                    = appViewTitleBar.ButtonInactiveBackgroundColor
                        = appViewTitleBar.ButtonBackgroundColor
                            = appViewTitleBar.BackgroundColor
                                = SplashScreenBackgroundColor;

            appViewTitleBar.ButtonForegroundColor
                = appViewTitleBar.ForegroundColor
                = ForegroundColorDarkTheme;
        }

        private static async void EnterMobileView()
        {
            // Set Status Bar
            var statusBar = StatusBar.GetForCurrentView();
            await statusBar.ShowAsync();

            statusBar.BackgroundColor = SplashScreenBackgroundColor;
        }

        public static void SetTitleBarText(string title)
        {
            // Get current window
            var appView = ApplicationView.GetForCurrentView();
            appView.Title = title;

            TitleChanged?.Invoke(null, title);
        }

        public static event EventHandler<string> TitleChanged;
    }
}
