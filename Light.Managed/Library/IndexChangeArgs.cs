using System.Collections.Generic;
using Light.Managed.Database.Entities;

namespace Light.Managed.Library
{
    /// <summary>
    /// Intended for communication between LibraryService and Indexer Only.
    /// </summary>
    public class IndexChangeArgs
    {
        public IndexChangeType ActionType { get; }
        public IndexContentType ContentType { get; } 
        public IEnumerable<DbMediaFile> MediaFiles { get; }
        public IEnumerable<DbArtist> Artists { get; }
        public IEnumerable<DbAlbum> Albums { get; }

        public IndexChangeArgs(IndexChangeType actionType, IEnumerable<DbMediaFile> files)
        {
            ActionType = actionType;
            ContentType = IndexContentType.File;
            MediaFiles = files;
        }

        public IndexChangeArgs(IndexChangeType actionType, IEnumerable<DbArtist> artists)
        {
            ActionType = actionType;
            ContentType = IndexContentType.Artist;
            Artists = artists;
        }

        public IndexChangeArgs(IndexChangeType actionType, IEnumerable<DbAlbum> albums)
        {
            ActionType = actionType;
            ContentType = IndexContentType.Album;
            Albums = albums;
        }
    }

    public enum IndexChangeType
    {
        Add = 0,
        Delete = 1,
        Modify = 2
    }

    public enum IndexContentType
    {
        File = 0,
        Album = 1,
        Artist = 2,
        Search = 3
    }
}
