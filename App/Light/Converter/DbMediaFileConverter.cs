using System;
using Windows.Media.Core;
using Windows.UI.Xaml.Data;
using Light.Managed.Database.Entities;
using FrameView = Light.View.Core.FrameView;
using Light.Model;

namespace Light.Converter
{
    internal class DbMediaFileConverter
    {
        public static DbMediaFile Convert(MediaSource value)
        {
            return (DbMediaFile)value.CustomProperties[Light.Core.PlaybackControl.DbMediaFileToken];
        }
    }

    public class MediaSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is MediaSource)
            {
                return DbMediaFileConverter.Convert((MediaSource)value);
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DbMediaFileToIndexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DbMediaFile)
            {
                var source = (DbMediaFile)value;
                int /*discNumber = 0, */trackNumber = 0;
                //int.TryParse(source.DiscNumber, out discNumber);
                int.TryParse(source.TrackNumber, out trackNumber);
                /*if (discNumber > 0)
                {
                    return $"{discNumber}.{trackNumber}";
                }
                else */
                if (trackNumber > 0)
                {
                    return $"{trackNumber}.";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DbMediaFileToImageTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var val = (DbMediaFile)value;
            return new ThumbnailTag
            {
                Fallback = "Album,AlbumPlaceholder",
                AlbumName = val.Album,
                ArtistName = val.AlbumArtist,
                ThumbnailPath = val.Path
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class TimeSpanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is TimeSpan)
            {
                var source = (TimeSpan)value;
                return $"{(int)source.TotalMinutes}:{source.Seconds:00}";
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
