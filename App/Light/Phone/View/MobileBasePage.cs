using Light.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Light.Phone.View
{
    public abstract class MobileBasePage : Page
    {
        private readonly NavigationHelper Navigation;
        public MobileBasePage()
        {
            Navigation = new NavigationHelper(this);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Navigation.OnNavigatedTo(e);
            base.OnNavigatedTo(e);
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Navigation.OnNavigatedFrom(e);
            base.OnNavigatedFrom(e);
        }
        public virtual bool ShowPlaybackControl => true;
        public virtual bool ReserveSpaceForStatusBar => true;
    }
}
