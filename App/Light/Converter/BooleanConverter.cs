using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Light.Converter
{
    public class NullableBooleanToBoolean : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (value as bool?) == true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (bool)value;
        }
    }

    public class ReverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool) return !(bool) value;
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool) return !(bool)value;
            throw new NotSupportedException();
        }
    }

    public class SplitViewStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                return ((bool) value) ? SplitViewDisplayMode.Inline : SplitViewDisplayMode.Overlay;
            }
            return SplitViewDisplayMode.Overlay;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is SplitViewDisplayMode)
            {
                switch ((SplitViewDisplayMode) value)
                {
                    case SplitViewDisplayMode.Overlay:
                        return false;
                    case SplitViewDisplayMode.Inline:
                        return true;
                }
            }
            return false;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                return (bool)value ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                return Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
