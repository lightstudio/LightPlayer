using System;
using System.Windows.Input;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;
using Light.Core;
using Light.Flyout;
using Light.Managed.Database.Entities;

namespace Light.ViewModel.Library.Commands
{
    public class PlayMenuCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return parameter is int  || parameter is DbMediaFile;
        }

        public virtual async void Execute(object parameter)
        {
            if (parameter is int)
            {
                PlaybackControl.Instance.Clear();
                await PlaybackControl.Instance.AddFile((int)parameter);
            }
            else if (parameter is DbMediaFile)
            {
                PlaybackControl.Instance.Clear();
                await PlaybackControl.Instance.AddFile(
                    MusicPlaybackItem.CreateFromMediaFile((DbMediaFile)parameter));
            }
        }

#pragma warning disable CS0067 // Reserved for XAML Framework
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }

    public class PlayAsNextMenuCommand : PlayMenuCommand
    {
        public override async void Execute(object parameter)
        {
            if (parameter is int)
            {
                await PlaybackControl.Instance.AddFile((int)parameter, -2);
            }
            else if (parameter is DbMediaFile)
            {
                await PlaybackControl.Instance.AddFile(
                    MusicPlaybackItem.CreateFromMediaFile((DbMediaFile)parameter), -2);
            }
        }
    }

    public class AddToPlaylistCommand : PlayMenuCommand
    {
        public override async void Execute(object parameter)
        {
            if (parameter is int)
            {
                await PlaybackControl.Instance.AddFile((int)parameter);
            }
            else if (parameter is DbMediaFile)
            {
                await PlaybackControl.Instance.AddFile(
                    MusicPlaybackItem.CreateFromMediaFile((DbMediaFile)parameter));
            }
        }
    }

    public class OpenFileLocationCommand : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return parameter is int || parameter is DbMediaFile;
        }

        public async void Execute(object parameter)
        {
            DbMediaFile item = parameter as DbMediaFile;

            if (item == null && (!(parameter is int) || 
                (item = (((int)parameter)).GetFileById()) == null))
                return;

            try
            {
                var folderPath = System.IO.Path.GetDirectoryName(item.Path);
                var folder = await StorageFolder.GetFolderFromPathAsync(folderPath);

                if (folder != null) await LaunchFolder(folder, item);
            }
            catch
            {
                // Ignore
            }
        }

        private async Task LaunchFolder(StorageFolder folder, DbMediaFile item)
        {
            await Launcher.LaunchFolderAsync(folder, new FolderLauncherOptions
            {
                ItemsToSelect = { await StorageFile.GetFileFromPathAsync(item.Path) }
            });
        }

#pragma warning disable CS0067 // Reserved for XAML Framework
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }

    internal class ShowFilePropertiesPopupCommand : ICommand
    {
        public bool CanExecute(object parameter) => parameter is int;

        public async void Execute(object parameter)
        {
            if (parameter is int)
            {
                await MediaFilePropertiesDialog.ShowFilePropertiesViewAsync((int) parameter);
            }
        }

#pragma warning disable CS0067 // Reserved for XAML Framework
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }
}
