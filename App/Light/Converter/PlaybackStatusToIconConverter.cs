using System;
using Windows.Media;
using Windows.UI.Xaml.Data;
using Light.Common;

namespace Light.Converter
{
    class PlaybackStatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value.GetType() != typeof (MediaPlaybackStatus)) return "";
            var stat = (MediaPlaybackStatus) value;
            switch (stat)
            {
                case MediaPlaybackStatus.Changing:
                    return CommonSharedStrings.ChangeTextGlyph;
                case MediaPlaybackStatus.Closed:
                    return "";
                case MediaPlaybackStatus.Paused:
                    return CommonSharedStrings.PauseTextGlyph;
                case MediaPlaybackStatus.Playing:
                    return CommonSharedStrings.PlayingTextGlyph;
                case MediaPlaybackStatus.Stopped:
                    return "";
            }
            return "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotSupportedException();
        }
    }
}
