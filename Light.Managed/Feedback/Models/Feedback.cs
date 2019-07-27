using System;
using System.ComponentModel.DataAnnotations;

namespace Light.Managed.Feedback.Models
{
    /// <summary>
    /// Feedback entity.
    /// </summary>
    public class Feedback
    {
        /// <summary>
        /// Required: Feedback request ID.
        /// </summary>
        [Required]
        public Guid RequestId { get; set; }

        /// <summary>
        /// Required: request time.
        /// </summary>
        [Required]
        public DateTime RequestTimeUtc { get; set; }

        /// <summary>
        /// Required: feedback title.
        /// </summary>
        [Required]
        public string Title { get; set; }

        /// <summary>
        /// Required: feedback content.
        /// </summary>
        [Required]
        public string Content { get; set; }

        /// <summary>
        /// Required: feedback type.
        /// </summary>
        [Required]
        public FeedbackType Type { get; set; }

        /// <summary>
        /// Optional: Telemetry metadata.
        /// </summary>
        public ApplicationInsightInfo TelemetryMetadata { get; set; }

        /// <summary>
        /// Required: Runtime information.
        /// </summary>
        public RuntimeInfo Runtime { get; set; }

        /// <summary>
        /// Optional: Image Id.
        /// </summary>
        public Guid ImageId { get; set; }

        /// <summary>
        /// Optional: Contact info.
        /// </summary>
        public string ContactInfo { get; set; }
    }

    /// <summary>
    /// Optional Application insight telemetry info
    /// </summary>
    public class ApplicationInsightInfo
    {
        /// <summary>
        /// Optional: Application Insight unique device ID.
        /// </summary>
        [Required]
        public string UniqueDeviceId { get; set; }

        /// <summary>
        /// Optional: Application Insight unique user ID.
        /// </summary>
        [Required]
        public string UniqueUserId { get; set; }

        /// <summary>
        /// Optional: Application Insight unique instrumentation ID.
        /// </summary>
        [Required]
        public string UniqueInstrumentationId { get; set; }
    }

    /// <summary>
    /// Runtime information.
    /// </summary>
    public class RuntimeInfo
    {
        [Required]
        public string ApplicationVersion { get; set; }

        [Required]
        public string OsReleaseVersion { get; set; }

        [Required]
        public string CurrentLanguage { get; set; }

        [Required]
        public string DeviceFamily { get; set; }
    }
}
