using Light.Managed.Database.Entities;
using Light.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Light.Core
{
    public class MusicPlaybackItem
    {
        public DbMediaFile File { get; set; }
        public LinkedListNode<MusicPlaybackItem> Node { get; set; }
        public string Title => File.Title;
        public string Album => File.Album;
        public string Artist => File.Artist;
        public double Duration => File.Duration.TotalMilliseconds;
        public string ArtistAlbum
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Album))
                {
                    return Artist;
                }
                else if (string.IsNullOrWhiteSpace(Artist))
                {
                    return Album;
                }
                else
                {
                    return $"{Artist}, {Album}";
                }
            }
        }
        public ThumbnailTag AlbumImageTag
        {
            get
            {
                return new ThumbnailTag
                {
                    Fallback = "Album,AlbumPlaceholder",
                    AlbumName = Album,
                    ArtistName = Artist,
                    ThumbnailPath = File.Path
                };
            }
        }
        public ThumbnailTag ArtistImageTag
        {
            get
            {
                return new ThumbnailTag
                {
                    Fallback= "Artist,Album,DefaultArtistLarge",
                    ArtistName = Artist,
                    AlbumName = Album,
                    ThumbnailPath = File.Path
                };
            }
        }
        static public MusicPlaybackItem CreateFromMediaFile(DbMediaFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }
            return new MusicPlaybackItem
            {
                File = file
            };
        }
    }
}
