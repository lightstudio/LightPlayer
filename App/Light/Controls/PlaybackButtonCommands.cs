using System;
using System.Windows.Input;
using Light.Common;
using Light.Core;

namespace Light.Controls
{
    public class PlaybackButtonCommands : ICommand
    {
        public bool CanExecute(object parameter)
        {
            return parameter is string;
        }

        public void Execute(object parameter)
        {
            switch ((string) parameter)
            {
                case CommonSharedStrings.PlayOrPause:
                    Core.PlaybackControl.Instance.PlayOrPause();
                    break;
                case CommonSharedStrings.Next:
                    Core.PlaybackControl.Instance.Next();
                    break;
                case CommonSharedStrings.Prev:
                    Core.PlaybackControl.Instance.Prev();
                    break;
            }
        }

#pragma warning disable CS0067
        public event EventHandler CanExecuteChanged;
#pragma warning restore CS0067
    }
}
