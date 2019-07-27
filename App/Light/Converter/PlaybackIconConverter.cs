using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Light.Converter
{
    class PlaybackIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value.GetType() != typeof(MediaElementState)) throw new NotSupportedException();
            switch ((MediaElementState) value)
            {
                case MediaElementState.Paused:
                    return Symbol.Play;
                case MediaElementState.Playing:
                    return Symbol.Pause;
                default:
                    return Symbol.Pause;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
