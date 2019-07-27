using System;
using Windows.UI.Xaml.Data;

namespace Light.Converter
{
    class MiliSecToNormalTimeConverter : IValueConverter
    {
        public static string GetTimeStringFromTimeSpanOrDouble(object value)
        {
            if (value.GetType() != typeof(double) && value.GetType() != typeof(TimeSpan)) throw new NotSupportedException();
            TimeSpan time;
            if (value is TimeSpan) time = (TimeSpan)value;
            else time = TimeSpan.FromMilliseconds((double)value);
            var h = (int)time.TotalHours;
            var m = time.Minutes;
            var s = time.Seconds;
            if (h < 0) h = 0;
            if (s < 0) s = 0;
            if (m < 0) m = 0;
            var hString = (h < 10) ? $"0{h}" : $"{h}";
            var mString = (m < 10) ? $"0{m}" : $"{m}";
            var sString = (s < 10) ? $"0{s}" : $"{s}";
            return h > 0 ? $"{hString}:{mString}:{sString}" : $"{mString}:{sString}";
        }
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return GetTimeStringFromTimeSpanOrDouble(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if(value.GetType() != typeof(string)) throw new NotSupportedException();
            var time = TimeSpan.Parse((string) value);
            return time.TotalMilliseconds;
        }
    }
}
