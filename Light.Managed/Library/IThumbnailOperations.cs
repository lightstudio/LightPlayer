using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Library
{
    public interface IThumbnailOperations
    {
        Task FetchAlbumAsync(string artist, string album, string filePath);
        Task RemoveAlbumAsync(string artist, string album);
        Task RemoveArtistAsync(string artist);
    }
}
