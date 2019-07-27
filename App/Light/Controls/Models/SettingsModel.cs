using Windows.UI.Xaml;
using GalaSoft.MvvmLight.Command;

namespace Light.Controls.Models
{
    public class FolderModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public RelayCommand<RoutedEventArgs> RemoveFolderButtonClickedRelayCommand { get; set; }
    }

    public class LrcSourceModel
    {
        public string Name { get; set; }
        public RelayCommand<RoutedEventArgs> RemoveLrcSourceButtonClickedRelayCommand { get; set; }
    }
}
