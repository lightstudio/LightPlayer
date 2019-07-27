using Light.Common;
using Light.Model;
using Light.ViewModel.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Flyout
{
    public sealed partial class ThumbnailSearchFlyout : ContentDialog
    {
        ThumbnailSearchViewModel ViewModel => (ThumbnailSearchViewModel)DataContext;
        public ThumbnailSearchFlyout(CommonViewItemModel model)
        {
            this.InitializeComponent();
            if (model.Type == CommonItemType.Album)
            {
                DataContext = new ThumbnailSearchViewModel(model.ExtendedArtistName, model.Title);
            }
            else
            {
                DataContext = new ThumbnailSearchViewModel(model.Title);
            }
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            args.Cancel = true;
            if (await ViewModel.DownloadSelected())
            {
                Hide();
            }
        }

        private async void OnSearchClicked(object sender, RoutedEventArgs e)
        {
            await ViewModel.SearchAsync();
        }

        private async void OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await ViewModel.SearchAsync();
        }

        private async void OnClearClick(object sender, RoutedEventArgs e)
        {
            await ViewModel.ClearAsync();
            Hide();
        }

        private async void OnImportClick(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(CommonSharedStrings.JpgFileSuffix);
            picker.FileTypeFilter.Add(CommonSharedStrings.PngFileSuffix);
            picker.CommitButtonText = CommonSharedStrings.ManualSelectLyricButtonText;
            var imageFile = await picker.PickSingleFileAsync();
            if (imageFile == null)
            {
                return;
            }
            ViewModel.IsBusy = true;
            try
            {
                using (var stream = await imageFile.OpenStreamForReadAsync())
                using (var sr = new BinaryReader(stream))
                {
                    if (stream.Length > 10 * 1024 * 1024)
                    {
                        throw new Exception(CommonSharedStrings.FileSizeLimitPrompt);
                    }
                    var content = sr.ReadBytes((int)stream.Length);
                    await ViewModel.ImportAsync(content);
                }
                Hide();
            }
            catch (Exception ex)
            {
                ViewModel.ResultText = string.Format(CommonSharedStrings.SearchError, ex.Message);
            }
            finally
            {
                ViewModel.IsBusy = false;
            }
        }
    }
}
