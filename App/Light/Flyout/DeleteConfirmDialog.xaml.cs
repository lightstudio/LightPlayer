using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;
using Light.Common;

namespace Light.Flyout
{
    public sealed partial class DeleteConfirmDialog : ContentDialog
    {
        public DeleteConfirmDialog(string itemName)
        {
            this.InitializeComponent();
            var resLoader = ResourceLoader.GetForCurrentView();
            DeleteConfirmTextBlock.Text = string.Format(CommonSharedStrings.DeleteConfirmationText, itemName);
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
