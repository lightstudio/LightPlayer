using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Controls;
using Light.Common;
using Light.Lyrics;
using LightLrcComponent;
using Light.Lyrics.Model;
using Light.Managed.Tools;
using Light.ViewModel.Utils;

namespace Light.Flyout
{
    public sealed class LrcSelectedEventArgs : EventArgs
    {
        public ParsedLrc Lrc { get; set; }
    }

    public sealed partial class LyricManualSelectionFlyout : ContentDialog
    {
        public event EventHandler<LrcSelectedEventArgs> LrcSelected;
        private LyricManualSelectionViewModel _vm;

        public LyricManualSelectionFlyout()
        {
            this.InitializeComponent();
        }

        internal IAsyncOperation<ContentDialogResult> ShowAsync(string title, string artist, IList<ExternalLrcInfo> candidates)
        {
            _vm = new LyricManualSelectionViewModel(title, artist, candidates);
            DataContext = _vm;
            LyricsCandidateListView.DataContext = _vm;
            return ShowAsync();
        }

        private async void OnPrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            try
            {
                args.Cancel = true;
                _vm.IsBusy = true;
                var lrc = await _vm.DownloadAsync();
                if (lrc == null ||
                    lrc.Sentences == null ||
                    lrc.Sentences.Count == 0)
                {
                    _vm.ResultText = CommonSharedStrings.LrcNotFound;
                    return;
                }
                LrcSelected?.Invoke(this,
                    new LrcSelectedEventArgs { Lrc = lrc });
                Hide();
            }
            catch (Exception ex)
            {
                _vm.ResultText = string.Format(CommonSharedStrings.LrcDownloadFailed, ex.Message);
            }
            finally
            {
                _vm.IsBusy = false;
            }
        }
        
        private async void ExternalLyricsImportLinkClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            if (_vm == null) return;
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(CommonSharedStrings.LrcFileSuffix);
            picker.FileTypeFilter.Add(CommonSharedStrings.TxtFileSuffix);
            picker.CommitButtonText = CommonSharedStrings.ManualSelectLyricButtonText;
            var lyricFile = await picker.PickSingleFileAsync();
            if (lyricFile == null)
                return;
            LrcSelected?.Invoke(this,
                new LrcSelectedEventArgs
                {
                    Lrc = await _vm.ImportAsync(lyricFile)
                });
            Hide();
        }

        private async void ClearLyricLinkClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            try
            {
                if (_vm == null) return;
                await _vm.DeleteAsync();
                LrcSelected?.Invoke(this, new LrcSelectedEventArgs { Lrc = null });
                Hide();
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

    }
}
