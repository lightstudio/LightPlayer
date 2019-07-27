using Windows.Media;
using GalaSoft.MvvmLight;
using Light.Managed.Database.Entities;

namespace Light.Model
{
    internal class PlaylistEntity : ViewModelBase
    {
        private MediaPlaybackStatus _status;
        public MediaPlaybackStatus Status
        {
            get { return _status; }
            set
            {
                if (_status == value) return;
                _status = value;
                RaisePropertyChanged();
            }
        }

        public DbMediaFile OriginEntity { get; }

        public PlaylistEntity(DbMediaFile file)
        {
            Status = MediaPlaybackStatus.Closed;
            OriginEntity = file;
        }
    }
}
