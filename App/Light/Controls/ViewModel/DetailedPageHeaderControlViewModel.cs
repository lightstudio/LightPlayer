using System;
using System.Linq;
using System.Windows.Input;
using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Core;
using Light.Managed.Database;
using Light.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Light.Controls.ViewModel
{
    public class PlaybackAppBarCommandEventArgs
    {
        public CommonItemType Type { get; set; }
        public int EntityId { get; set; }
        public string Command { get; set; }

        public PlaybackAppBarCommandEventArgs()
        {
            Type = CommonItemType.Other;
            EntityId = -1;
            Command = string.Empty;
        }
    }

    public class PlaybackAppBarCommand : ICommand
    {
        public CommonItemType Type { get; set; }
        public int EntityId { get; set; }

        public bool CanExecute(object parameter) => parameter is string || parameter is PlaybackAppBarCommandEventArgs;

        public void Execute(object parameter)
        {
            if (parameter is string)
            {
                HandleStringRoute((string) parameter);
            }
            else if (parameter is PlaybackAppBarCommandEventArgs)
            {
                var param = (PlaybackAppBarCommandEventArgs) parameter;
                Type = param.Type;
                EntityId = param.EntityId;
                HandleStringRoute(param.Command);
            }
        }

        public void HandleStringRoute(string parameter)
        {
            switch (parameter)
            {
                case CommonSharedStrings.Share:
                    Messenger.Default.Send(new MessageBase(), 
                        CommonSharedStrings.ControlPageShareClickedEventMessageToken);
                    break;
                case CommonSharedStrings.Play:
                    HandlePlay(true);
                    break;
                case CommonSharedStrings.AddToNowPlaying:
                    HandlePlay();
                    break;
            }
        }

        private void HandlePlay(bool requireClear = false, bool isInsert = false)
        {
            switch (Type)
            {
                case CommonItemType.Album:
                    HandleAlbumPlay(requireClear, isInsert);
                    break;
                case CommonItemType.Artist:
                    HandleArtistPlay(requireClear, isInsert);
                    break;
            }
        }

        private void HandleArtistPlay(bool requireClear, bool isInsert)
        {
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var artist = context.Artists
                    .Include(c => c.MediaFiles)
                    .First(i => i.Id == EntityId);
                artist.Play(requireClear, isInsert);
            }
        }

        private void HandleAlbumPlay(bool requireClear, bool isInsert)
        {
            using (var scope = ApplicationServiceBase.App.GetScope())
            using (var context = scope.ServiceProvider.GetRequiredService<MedialibraryDbContext>())
            {
                var album = context.Albums
                    .Include(c => c.MediaFiles)
                    .First(i => i.Id == EntityId);
                album.Play(requireClear, isInsert);
            }
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }
}
