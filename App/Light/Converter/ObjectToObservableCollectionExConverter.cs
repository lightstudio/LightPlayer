using System;
using Windows.UI.Xaml.Data;
using Light.Model;
using Light.Utilities;

namespace Light.Converter
{
    public class ObjectToObservableCollectionExConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (ObservableCollectionEx<PlaylistEntity>) value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (ObservableCollectionEx<PlaylistEntity>)value;
        }
    }
}
