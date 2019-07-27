using Light.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Light.View.Core
{
    public class BaseContentPage : Page
    {
        public string PageTitle
        {
            get { return (string)GetValue(PageTitleProperty); }
            set
            {
                FrameView.Current?.SetTitleString(value);
                SetValue(PageTitleProperty, value);
            }
        }

        public static readonly DependencyProperty PageTitleProperty =
            DependencyProperty.Register(nameof(PageTitle), typeof(string), typeof(BaseContentPage), new PropertyMetadata(CommonSharedStrings.MyMusicUpper));

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            FrameView.Current?.SetTitleString(PageTitle);
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            FrameView.Current?.SetTitleString(CommonSharedStrings.MyMusicUpper);
            base.OnNavigatedFrom(e);
        }
    }
}
