using System;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace Light.Managed.Feedback.Models
{
    /// <summary>
    /// Image upload result class.
    /// </summary>
    public class ImageUploadResult
    {
        [Required]
        public Guid ImageId { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="imageId"></param>
        public ImageUploadResult(Guid imageId)
        {
            ImageId = imageId;
        }

        [JsonConstructor]
        public ImageUploadResult()
        {
            
        }
    }
}
