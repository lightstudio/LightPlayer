using Windows.Media;
using Light.Managed.Database.Entities;

namespace Light.DataObjects
{
    internal class StatusChangedEventArgs
    {
		public DbMediaFile CurrentItem { get; set; }
		public int CurrentIndex { get; set; }
		public MediaPlaybackStatus Status { get; set; }

        public StatusChangedEventArgs(DbMediaFile currentItem, int currentIndex, MediaPlaybackStatus status)
        {
            CurrentIndex = currentIndex;
            CurrentItem = currentItem;
            Status = status;
        }
    }
}
