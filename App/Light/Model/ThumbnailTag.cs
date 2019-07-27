using System;

namespace Light.Model
{
    public class ThumbnailTag
    {
        /// <summary>
        /// Specify ThumbnailType in a string split by ','.
        /// MediaThumbnail will try to load the image form the first item.
        /// If the image loading failed, it will fallback to load the next item.
        /// If the image loading is delayed, it will display the last fallback item.
        /// MediaThumbnail will listen to image changes when image loading is delayed.
        /// When an event handler is fired, it will check current loading stage.
        /// </summary>
        /// <example>
        /// Fallback = "Artist,Album,DefaultArtistLarge"
        /// </example>
        public string Fallback { get; set; }

        public string ThumbnailPath { get; set; }

        public string ArtistName { get; set; }

        public string AlbumName { get; set; }
    }
}