using Windows.UI.Xaml.Media;

namespace Light.DataObjects
{
    public class PlayerStatus
    {
        public bool IsInPlayerablePageSet { get; set; }
        public bool IsStatusSet { get; set; }
        public bool IsFailed { get; set; }
        public MediaElementState Status { get; set; }
        public bool IsInPlayerablePage { get; set; }

        public PlayerStatus()
        {
            IsFailed = false;
            Status = MediaElementState.Closed;
            IsInPlayerablePage = false;
            IsStatusSet = false;
            IsInPlayerablePageSet = false;
        }
    }
}
