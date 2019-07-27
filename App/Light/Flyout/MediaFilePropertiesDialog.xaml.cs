using Light.Common;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Light.ViewModel.Library;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Light.Flyout
{
    /// <summary>
    /// Content dialog that presents file metadata information.
    /// </summary>
    public sealed partial class MediaFilePropertiesDialog : ContentDialog
    {
        public MediaFilePropertiesViewModel ViewModel => (MediaFilePropertiesViewModel)DataContext;

        /// <summary>
        /// Class constructor that creates instance of <see cref="MediaFilePropertiesDialog"/>.
        /// </summary>
        /// <param name="file">Instance of <see cref="StorageFile"/> that represents the file to retrieve metadata from.</param>
        public MediaFilePropertiesDialog(StorageFile file)
        {
            InitializeComponent();
            DataContext = new MediaFilePropertiesViewModel(file);
        }

        /// <summary>
        /// Show file properties asynchronously.
        /// </summary>
        /// <param name="databaseId">Entity ID from database.</param>
        public static async Task ShowFilePropertiesViewAsync(int databaseId)
        {
            DbMediaFile item = null;
            try
            {
                // Note: We would like to do some tricky things here in order to achieve better user experience.
                item = await databaseId.GetFileByIdAsync();
                var file = await StorageFile.GetFileFromPathAsync(item.Path);
                await ShowFilePropertiesViewAsync(file);
            }
            catch (FileNotFoundException)
            {
                if (item != null)
                {
                    var dialog = new MessageDialog(
                        string.Format(CommonSharedStrings.FileNotFound, item.Path),
                        CommonSharedStrings.Error);
                    await dialog.ShowAsync();
                }
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.Error);
                await dialog.ShowAsync();
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        public static async Task ShowFilePropertiesViewAsync(StorageFile file)
        {
            try
            {
                var dialog = new MediaFilePropertiesDialog(file);
                if (PlatformInfo.CurrentPlatform == Platform.WindowsMobile)
                {
                    var visibleBounds = ApplicationView.GetForCurrentView().VisibleBounds;
                    dialog.MaxHeight = 2 * visibleBounds.Height - visibleBounds.Top - visibleBounds.Bottom - 40;
                }
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                var dialog = new MessageDialog(ex.Message, CommonSharedStrings.Error);
                await dialog.ShowAsync();
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.LoadDataAsync();
        }
    }
}
