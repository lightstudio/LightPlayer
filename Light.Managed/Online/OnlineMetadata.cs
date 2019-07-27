using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Online
{
    public interface IEntityInfo
    {
        string AlbumName { get; }
        string ArtistName { get; }
        Uri Thumbnail { get; }
    }
}
