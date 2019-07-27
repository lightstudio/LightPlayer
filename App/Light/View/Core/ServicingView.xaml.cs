using Light.Common;
using Light.Core;
using Light.Managed.Servicing;
using Light.Phone.View;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Core
{
    /// <summary>
    /// View that shows servicing status.
    /// </summary>
    public sealed partial class ServicingView : Page
    {

        private object m_args = null;

        /// <summary>
        /// Initializes new instance of <see cref="ServicingView"/>.
        /// </summary>
        public ServicingView()
        {
            this.InitializeComponent();
            this.Loaded += OnServicingLoaded;
        }

        /// <summary>
        /// Handles page load.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Instance of <see cref="RoutedEventArgs"/>.</param>
        private async void OnServicingLoaded(object sender, RoutedEventArgs e)
        {
            await OnlineServicingManager.RunAsync();
            OnlineServicingManager.Commit();

            // Pass through arguments
            Frame.Navigate(DetermineNavigationPageType(), m_args);

            // Re-trigger indexing
            await LibraryService.IndexAsync(new ThumbnailOperations());

            // Unreg events
            this.Loaded -= OnServicingLoaded;
        }

        /// <summary>
        /// Handles page events on navigation.
        /// </summary>
        /// <param name="e">Instance of <see cref="NavigationEventArgs"/>.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            m_args = e.Parameter;
        }

        /// <summary>
        /// Determines navigation page type.
        /// </summary>
        /// <returns>Destination page type.</returns>
        private static Type DetermineNavigationPageType()
        {
            if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
            {
                return typeof(MobileFrameView);
            }
            else
            {
                return typeof(FrameView);
            }
        }
    }
}
