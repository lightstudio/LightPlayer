using System;
using System.IO;
using Windows.ApplicationModel;
using Windows.Media;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Light.Managed.Database.Entities;
using Light.Utilities.UserInterfaceExtensions;
using Windows.Media.Playback;
using Windows.Media.Core;

namespace Light.Core
{
    internal class UvcServices
    {
        private readonly SystemMediaTransportControls m_smtControl;
        private readonly CoreDispatcher m_dispatcher;
        private RandomAccessStreamReference m_defaultPicture;
        private MediaPlayer m_stubPlayer;

        public bool IsEnabled
        {
            get { return m_smtControl.IsEnabled; }
            set { m_smtControl.IsEnabled = value; }
        }

        public bool IsPrevEnabled
        {
            get { return m_smtControl.IsPreviousEnabled; }
            set { m_smtControl.IsPreviousEnabled = value; }
        }

        public bool IsNextEnabled
        {
            get { return m_smtControl.IsNextEnabled; }
            set { m_smtControl.IsNextEnabled = value; }
        }

        public bool IsPlayEnabled
        {
            get { return m_smtControl.IsPlayEnabled; }
            set { m_smtControl.IsPlayEnabled = value; }
        }

        public bool IsPauseEnabled
        {
            get { return m_smtControl.IsPauseEnabled; }
            set { m_smtControl.IsPauseEnabled = value; }
        }

        public MediaPlaybackStatus Status
        {
            get { return m_smtControl.PlaybackStatus; }
            set { m_smtControl.PlaybackStatus = value; }
        }

        private async void SmtControlOnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (!m_dispatcher.HasThreadAccess)
            {
                await m_dispatcher.RunAsync(CoreDispatcherPriority.High, () => SmtControlOnButtonPressed(sender, args));
                return;
            }

            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    PlaybackControl.Instance.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    PlaybackControl.Instance.Pause();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    PlaybackControl.Instance.Prev();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    PlaybackControl.Instance.Next();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    PlaybackControl.Instance.Stop();
                    break;
            }
        }

        public async void UpdateInfo(DbMediaFile file)
        {
            m_smtControl.DisplayUpdater.Type = MediaPlaybackType.Music;
            m_smtControl.DisplayUpdater.MusicProperties.Title = file.Title;
            m_smtControl.DisplayUpdater.MusicProperties.AlbumArtist = file.AlbumArtist;
            m_smtControl.DisplayUpdater.MusicProperties.AlbumTitle = file.Album;
            m_smtControl.DisplayUpdater.MusicProperties.Artist = file.Artist;
            try
            {
                var coverStream = await ThumbnailCache.RetrieveStorageFileAsStreamAsync(
                    await StorageFile.GetFileFromPathAsync(file.Path), true);
                if (coverStream != null)
                {
                    using (coverStream)
                    {
                        m_smtControl.DisplayUpdater.Thumbnail =
                            RandomAccessStreamReference.CreateFromStream(coverStream);
                        m_smtControl.DisplayUpdater.Update();
                        return;
                    }
                }
            }
            catch { }

            if (m_defaultPicture == null)
            {
                var coverPath = Path.Combine(
                    Package.Current.InstalledLocation.Path, "Assets", "DefaultCover.png");
                m_defaultPicture = RandomAccessStreamReference.CreateFromFile(
                    await StorageFile.GetFileFromPathAsync(coverPath));
            }
            m_smtControl.DisplayUpdater.Thumbnail = m_defaultPicture;
            m_smtControl.DisplayUpdater.Update();
        }

        public UvcServices(SystemMediaTransportControls control)
        {
            m_smtControl = control;
            m_smtControl.IsEnabled = true;
            m_smtControl.ButtonPressed += SmtControlOnButtonPressed;
            m_dispatcher = Window.Current.Dispatcher;
        }

        public UvcServices(MediaPlayer player)
        {
            player.CommandManager.IsEnabled = false;
            player.AutoPlay = false;
            player.Source = MediaSource.CreateFromUri(new Uri("ms-appx:///Assets/silence.mp3"));
            player.Play();

            m_smtControl = player.SystemMediaTransportControls;
            m_smtControl.IsEnabled = true;
            m_smtControl.PlaybackStatus = MediaPlaybackStatus.Stopped;
            m_smtControl.ButtonPressed += SmtControlOnButtonPressed;
            m_dispatcher = Window.Current.Dispatcher;
            m_stubPlayer = player;
        }
    }
}
