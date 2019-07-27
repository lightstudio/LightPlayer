using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Light.Utilities.UserInterfaceExtensions
{
    public static class FocusExtension
    {
        public static bool GetIsFocused(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsFocusedProperty);
        }


        public static void SetIsFocused(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFocusedProperty, value);
        }


        public static readonly DependencyProperty IsFocusedProperty =
            DependencyProperty.RegisterAttached(
             "IsFocused", typeof(bool), typeof(FocusExtension),
             new PropertyMetadata(false, OnIsFocusedPropertyChanged));


        private static async void OnIsFocusedPropertyChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {
            var control = (Control)d;
            if (control == null)
                return;
            if ((bool)e.NewValue)
            {
                await Task.Delay(50);
                control.Focus(FocusState.Programmatic);
            }
        }
    }
}
