using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Light.Common;
using Light.ViewModel.Core;
using Light.View.Core;

namespace Light.View.InitialExpeience
{
    /// <summary>
    /// Dedicated settings page for online metadata.
    /// </summary>
    public sealed partial class OnlineMetadataSettingsView : BaseContentPage
    {
        /// <summary>
        /// Common navigation helper for state save and navigation integration.
        /// </summary>
        private readonly NavigationHelper _navigationHelper;

        private readonly OnlineMetadataSettingsViewModel _viewModel;

        public OnlineMetadataSettingsView()
        {
            InitializeComponent();
            _navigationHelper = new NavigationHelper(this);
            _viewModel = new OnlineMetadataSettingsViewModel();
        }

        /// <summary>
        /// Handle navigate to events and initialize variables like ViewModel.
        /// </summary>
        /// <param name="e">NavigationTo event arguments, including target data type.</param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            _navigationHelper.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
            await _viewModel.LoadSettingsDataAsync();
        }

        /// <summary>
        /// Handle navigate from events and cleanup all used variables.
        /// </summary>
        /// <param name="e">NavigateFrom event arguments, used by common navigation helper.</param>
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _viewModel.Cleanup();
            _navigationHelper.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }
    }
}
