using System.Collections;
using Windows.UI.Xaml;

namespace Light.Utilities.UserInterfaceExtensions
{
    public static class IsVisible
    {
        public static readonly DependencyProperty ConditionProperty =
            DependencyProperty.RegisterAttached(
                "Condition", typeof(bool), typeof(UIElement), new PropertyMetadata(true, OnConditionChanged));

        public static readonly DependencyProperty InversionProperty =
            DependencyProperty.RegisterAttached(
                "Inversion", typeof(bool), typeof(UIElement), new PropertyMetadata(false, OnConditionChanged));

        public static readonly DependencyProperty WhenNotEmptyProperty =
            DependencyProperty.RegisterAttached(
                "WhenNotEmpty", typeof(string), typeof(UIElement), new PropertyMetadata(false, OnConditionChanged));

        public static readonly DependencyProperty WhenEmptyProperty =
            DependencyProperty.RegisterAttached(
                "WhenEmpty", typeof(string), typeof(UIElement), new PropertyMetadata(false, OnConditionChanged));

        public static bool GetCondition(UIElement target) =>
            (bool)target.GetValue(ConditionProperty);

        public static void SetCondition(UIElement target, bool value) =>
            target.SetValue(ConditionProperty, value);

        public static bool GetInversion(UIElement target) =>
            (bool)target.GetValue(InversionProperty);

        public static void SetInversion(UIElement target, bool value) =>
            target.SetValue(InversionProperty, value);

        public static string GetWhenNotEmpty(UIElement target) =>
            (string)target.GetValue(WhenNotEmptyProperty);

        public static void SetWhenNotEmpty(UIElement target, string value) =>
            target.SetValue(WhenNotEmptyProperty, value);

        public static string GetWhenEmpty(UIElement target) =>
            (string)target.GetValue(WhenEmptyProperty);

        public static void SetWhenEmpty(UIElement target, string value) =>
            target.SetValue(WhenEmptyProperty, value);

        static void OnConditionChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var element = o as UIElement;
            if (element == null) return;

            if (e.Property == InversionProperty)
            {
                element.Visibility = ((bool)e.NewValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.Property == ConditionProperty)
            {
                element.Visibility = ((bool)e.NewValue) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.Property == WhenNotEmptyProperty)
            {
                var str = e.NewValue as string;
                element.Visibility = string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (e.Property == WhenEmptyProperty)
            {
                var str = e.NewValue as string;
                element.Visibility = string.IsNullOrWhiteSpace(str) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    public static class HideIfUnset
    {
        public static readonly DependencyProperty BindingProperty =
            DependencyProperty.RegisterAttached(
                "Binding", typeof(object),
                typeof(UIElement), new PropertyMetadata(null, OnBindingChanged));

        public static object GetBinding(UIElement target) =>
            target.GetValue(BindingProperty);

        public static void SetBinding(UIElement target, object value) =>
            target.SetValue(BindingProperty, value);

        static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ICollection c;

            if (d == null) return;

            ((UIElement)d).Visibility = e.NewValue == null ||
                ((c = e.NewValue as ICollection) != null && c.Count < 1) ?
                Visibility.Collapsed : Visibility.Visible;
        }
    }
}
