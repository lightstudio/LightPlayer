using Light.Managed.Database.Native;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
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
    public sealed partial class LibraryWarningDialog : ContentDialog
    {
        class WarningModel
        {
            public string ExceptionMessage { get; set; }
            public string FileName { get; set; }
            public string ContainingPath { get; set; }
            public string FullPath { get; set; }
        }

        ObservableCollection<WarningModel> Warnings;
        public LibraryWarningDialog(List<Tuple<string, Exception>> exceptions)
        {
            this.InitializeComponent();
            Warnings = new ObservableCollection<WarningModel>(
                from exception
                in exceptions
                select new WarningModel
                {
                    ContainingPath = Path.GetDirectoryName(
                        exception.Item1.StartsWith("\\\\") ? exception.Item1 : "\\\\?\\" + exception.Item1),
                    ExceptionMessage = exception.Item2.Message,
                    FileName = Path.GetFileName(exception.Item1),
                    FullPath = exception.Item1
                });
        }

        private async void OnOpenFileClick(object sender, RoutedEventArgs e)
        {
            var model = (sender as FrameworkElement).DataContext as WarningModel;
            var file = await NativeMethods.GetStorageFileFromPathAsync(model.FullPath);
            var item = file as IStorageItem2;
            if (item == null)
                return;
            var parent = await item.GetParentAsync();
            if (parent != null)
            {
                await Launcher.LaunchFolderAsync(parent,
                    new FolderLauncherOptions
                    {
                        ItemsToSelect = { file }
                    });
            }
        }
    }
}
