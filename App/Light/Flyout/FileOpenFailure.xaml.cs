using Light.Common;
using Light.Core;
using Light.CueIndex;
using Light.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Content Dialog item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Light.Flyout
{
    class FileOpenFailureItem
    {
        private string _fullPath;
        public string FullPath => _fullPath;
        public string FileName => Path.GetFileName(_fullPath);
        public string ParentPath => _fullPath.Substring(0, _fullPath.Length - FileName.Length);
        public FileOpenFailureItem(string path)
        {
            _fullPath = path;
        }
    }


    public sealed partial class FileOpenFailure : ContentDialog
    {
        public static async Task<List<MusicPlaybackItem>> AddFailedFilePath(
            List<Tuple<StorageFile, string>> failedItems)
        {
            if (Current != null)
            {
                Current.Hide();
            }
            var source = new TaskCompletionSource<List<MusicPlaybackItem>>();
            await Windows.ApplicationModel.Core
                .CoreApplication.MainView.CoreWindow
                .Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                async () =>
                {
                    var dialog = new FileOpenFailure(failedItems);
                    await dialog.ShowAsync();
                    source.SetResult(dialog.Added);
                });
            return await source.Task;
        }

        static FileOpenFailure Current;

        ObservableCollection<FileOpenFailureItem> _items = new ObservableCollection<FileOpenFailureItem>();
        Dictionary<string, StorageFile> _failedItems = new Dictionary<string, StorageFile>();

        public List<MusicPlaybackItem> Added = new List<MusicPlaybackItem>();

        public FileOpenFailure(
            List<Tuple<StorageFile, string>> failedItems)
        {
            InitializeComponent();
            Opened += OnOpened;
            Closed += OnClosed;
            foreach (var item in failedItems)
            {
                _failedItems.Add(item.Item2, item.Item1);
            }
            foreach (var file in failedItems)
            {
                _items.Add(new FileOpenFailureItem(file.Item2));
            }
        }

        public void AddFiles(IEnumerable<string> files)
        {
            foreach (var file in files)
            {
                _items.Add(new FileOpenFailureItem(file));
            }
        }

        private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            Current = null;
        }

        private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            Current = this;
        }

        private void OnDragEntered(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                e.AcceptedOperation = DataPackageOperation.Copy;
            }
        }

        private async void OnDrop(object sender, DragEventArgs e)
        {
            var data = (e.OriginalSource as FrameworkElement)?.DataContext;

            if (data == null)
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var files = await FileOpen.GetAllFiles(items);
                List<string> _completed = new List<string>();
                foreach (var cue in _failedItems)
                {
                    if (await FileOpen.HandleCueFileOpen(cue.Value, files, Added) == null)
                    {
                        _completed.Add(cue.Key);
                    }
                }
                foreach (var str in _completed)
                {
                    _failedItems.Remove(str);
                    _items.Remove((from i
                                   in _items
                                   where string.Compare(i.FullPath, str, true) == 0
                                   select i).First());
                }
                if (_failedItems.Count == 0)
                    Hide();
            }
        }

        private async void OnSelectFileTapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            var item = (sender as FrameworkElement).DataContext as FileOpenFailureItem;
            var f = await PickMediaFileAsync();
            if (f != null)
            {
                FutureAccessListHelper.Instance.AddTempItem(new IStorageItem[] { f });

                var cueFile = _failedItems[item.FullPath];
                Added.AddRange(
                    await FileOpen.HandleFileWithCue(f,
                        await CueFile.CreateFromFileAsync(cueFile, false)));

                _failedItems.Remove(item.FullPath);
                _items.Remove((from i
                               in _items
                               where string.Compare(i.FullPath, item.FullPath, true) == 0
                               select i).First());

                if (_failedItems.Count == 0)
                    Hide();
            }
        }

        private static async Task<StorageFile> PickMediaFileAsync()
        {
            //wma is not supported.
            FileOpenPicker picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".flac");
            picker.FileTypeFilter.Add(".ape");
            picker.FileTypeFilter.Add(".tta");
            picker.FileTypeFilter.Add(".tak");
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");
            picker.CommitButtonText = CommonSharedStrings.Select;
            var file = await picker.PickSingleFileAsync();
            return file;
        }
    }
}
