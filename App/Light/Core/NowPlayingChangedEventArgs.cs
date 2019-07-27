using System;

namespace Light.Core
{
    public class NowPlayingChangedEventArgs : EventArgs
    {
        public MusicPlaybackItem NewItem { get; set; }
    }
}