using System;
using System.Windows.Input;
using Light.Common;
using Light.Core;
using Light.Model;

namespace Light.ViewModel.Library.Commands
{
    public class CommonGridViewItemCommand : ICommand
    {
        public bool CanExecute(object parameter) => parameter is CommonGridViewItemCommandArgs;

        public void Execute(object parameter)
        {
            if (parameter is CommonGridViewItemCommandArgs)
            {
                var entity = (CommonGridViewItemCommandArgs) parameter;
                switch (entity.Type)
                {
                    case CommonItemType.Song:
                        SongHandler(entity);
                        break;
                }
            }
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067

        private async void SongHandler(CommonGridViewItemCommandArgs entity)
        {
            switch (entity.Operation)
            {
                case CommonSharedStrings.Play:
                    PlaybackControl.Instance.Clear();
                    await PlaybackControl.Instance.AddFile(entity.EntityId);
                    break;
                case CommonSharedStrings.AddToPlaylist:
                    await PlaybackControl.Instance.AddFile(entity.EntityId);
                    break;
                case CommonSharedStrings.PlayAsNext:
                    await PlaybackControl.Instance.AddFile(entity.EntityId, -2);
                    break;
            }
            PlaybackControl.Instance.Play();
        }
    }

    public class CommonGridViewItemCommandArgs
    {
        public int EntityId { get; set; }
        public string Operation { get; set; }
        public CommonItemType Type { get; set; }
    }
}