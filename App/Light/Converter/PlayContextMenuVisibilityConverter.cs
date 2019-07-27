using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Light.Model;

namespace Light.Converter
{
    class PlayContextMenuVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType() != typeof (CommonItemType)) throw new NotSupportedException(nameof(value.GetType));
            return ((CommonItemType) value == CommonItemType.Artist) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if(value.GetType() != typeof(Visibility)) throw new NotSupportedException(nameof(value.GetType));
            return ((Visibility) value == Visibility.Visible) ? CommonItemType.Album : CommonItemType.Artist;
        }
    }
}
