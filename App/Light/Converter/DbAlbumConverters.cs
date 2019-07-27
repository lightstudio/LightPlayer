using Light.Managed.Database.Entities;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Light.Converter
{
    class DbAlbumToThumbnailTagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var album = (DbAlbum)value;
            return new ThumbnailTag
            {
                Fallback = "Album,AlbumPlaceholder",
                ArtistName = album.Artist,
                AlbumName = album.Title,
                ThumbnailPath = album.FirstFileInAlbum
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
