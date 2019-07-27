using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using Light.Managed.Feedback.Models;
using Light.Managed.Tools;
using Newtonsoft.Json;

namespace Light.Managed.Feedback
{
    /// <summary>
    /// Feedback client for TFS integration.
    /// </summary>
    public static class FeedbackClient
    {
        private const string RequestUrlPrefix = "https://appfeedback.ligstd.com:550";
        private const string JsonMimeType = "application/json";
        private const string PngMimeType = "image/png";
        private const string PngExtension = ".png";

        /// <summary>
        /// Upload image and get image ID.
        /// </summary>
        /// <param name="file">The image file to be uploaded.</param>
        /// <returns>An awaitable task with progress. Upon finishing, the image ID will be returned.</returns>
        public static IAsyncOperationWithProgress<ImageUploadResult, double> UploadImageAsync(this StorageFile file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            if (file.FileType != PngExtension)
                throw new Exception("Not a png image.");

            return AsyncInfo.Run<ImageUploadResult, double>((token, progress) =>
                Task.Run(async () =>
                {
                    ImageUploadResult rel;
                    var uploadEndpoint = $"{RequestUrlPrefix}/api/FeedbackImage";
                    var fileSize = (await file.GetBasicPropertiesAsync()).Size;

                    using (var httpClient = new HttpClient())
                    {
                        // Set default accept.
                        httpClient.DefaultRequestHeaders.Accept.Add(new HttpMediaTypeWithQualityHeaderValue(JsonMimeType));

                        // Prep file stream.
                        using (var fileRasStream = await file.OpenAsync(FileAccessMode.Read))
                        using (var fileInputStream = fileRasStream.GetInputStreamAt(0))
                        using (var fileStreamContent = new HttpStreamContent(fileInputStream))
                        {
                            fileStreamContent.Headers.ContentType = new HttpMediaTypeHeaderValue(PngMimeType);
                            fileStreamContent.Headers.ContentLength = fileSize;

                            // Send request
                            var uploadTask = httpClient.PostAsync(new Uri(uploadEndpoint), fileStreamContent);
                            uploadTask.Progress += (info, progressInfo) =>
                            {
                                 progress.Report((double) progressInfo.BytesSent / fileSize);
                            };

                            // Wait result
                            var result = await uploadTask;
                            if (result.StatusCode == HttpStatusCode.TooManyRequests)
                            {
                                throw new RequestThrottledException();
                            }
                            // For other scenarios, HTTP 200 is excepted
                            else if (result.StatusCode != HttpStatusCode.Ok)
                            {
                                throw new FeedbackServerErrorException();
                            }

                            var content = await result.Content.ReadAsStringAsync();
                            rel = JsonConvert.DeserializeObject<ImageUploadResult>(content);
                        }
                    }

                    return rel;
                }, token));
        }

        /// <summary>
        /// Send a feedback to telemetry server.
        /// </summary>
        /// <param name="type">Feedback type.</param>
        /// <param name="title">Feedback title.</param>
        /// <param name="content">Feedback content.</param>
        /// <param name="contactInfo">Contact information (if available)</param>
        /// <param name="image">Image (optional)</param>
        /// <returns>An awaitable task.</returns>
        public static async Task SendfeedbackAsync(FeedbackType type, string title, string content, string contactInfo,
            ImageUploadResult image = null)
        {
            var currentVersion =
                $"{Package.Current.Id.Version.Major}." +
                $"{Package.Current.Id.Version.Minor}." +
                $"{Package.Current.Id.Version.Build}." +
                $"{Package.Current.Id.Version.Revision}" +
                $"_{Package.Current.Id.Architecture}";

            var osVersion = ulong.Parse(AnalyticsInfo.VersionInfo.DeviceFamilyVersion);
            var osMajor = (osVersion & 0xFFFF000000000000L) >> 48;
            var osMinor = (osVersion & 0x0000FFFF00000000L) >> 32;
            var osBuild = (osVersion & 0x00000000FFFF0000L) >> 16;
            var osRev = osVersion & 0x000000000000FFFFL;

            var feedback = new Models.Feedback
            {
                Type = type,
                Title = title,
                Content = content,
                RequestId = Guid.NewGuid(),
                RequestTimeUtc = DateTime.UtcNow,
                Runtime = new RuntimeInfo
                {
                    ApplicationVersion = currentVersion,
                    CurrentLanguage = CultureInfo.CurrentCulture.Name,
                    DeviceFamily = AnalyticsInfo.VersionInfo.DeviceFamily,
                    OsReleaseVersion = $"{osMajor}.{osMinor}.{osBuild}.{osRev}"
                },
                ContactInfo = contactInfo
            };

            if (TelemetryHelper.OptinTelemetry)
            {
                // Track an event before getting info
                await TelemetryHelper.TraceEventAsync("Feedback", new Dictionary<string, string>
                {
                    {"FeedbackId", feedback.RequestId.ToString()}
                });

                feedback.TelemetryMetadata = new ApplicationInsightInfo
                {
                    UniqueDeviceId = TelemetryHelper.DeviceId,
                    UniqueInstrumentationId = TelemetryHelper.InstrumentationId,
                    UniqueUserId = TelemetryHelper.UserId
                };
            }

            if (image != null)
            {
                feedback.ImageId = image.ImageId;
            }

            // Serialize request
            var requestContent = JsonConvert.SerializeObject(feedback);

            // Send request
            using (var httpClient = new HttpClient())
            {
                var uploadEndpoint = $"{RequestUrlPrefix}/api/Feedback";
                var result = await httpClient.PostAsync(new Uri(uploadEndpoint), new HttpStringContent(
                    requestContent, UnicodeEncoding.Utf8, JsonMimeType));

                // Check if request is throttled.
                if (result.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new RequestThrottledException();
                }
                // For other scenarios, HTTP 200 is excepted
                else if (result.StatusCode != HttpStatusCode.Ok)
                {
                    throw new FeedbackServerErrorException();
                }
            }
        }
    }
}
