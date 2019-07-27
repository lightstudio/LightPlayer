using System;
using Windows.Foundation.Metadata;
using Windows.System.Profile;
using Windows.UI.Notifications;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using Microsoft.Toolkit.Uwp.Notifications;

namespace Light.Shell
{
    /// <summary>
    /// Action center integration for rs1+.
    /// </summary>
    public class ActionCenterIntegration
    {
        /// <summary>
        /// Send adaptive toast, including now playing item, to Action Center.
        /// </summary>
        /// <param name="file">To file to be sent.</param>
        public static void SendAdaptiveToast(DbMediaFile file)
        {
            if (file == null)
            {
                return;
            }
            if (IsAdaptiveToastSupported())
            {
                // Start by constructing the visual portion of the toast
                var binding = new ToastBindingGeneric();

                binding.AppLogoOverride = new ToastGenericAppLogo
                {
                    Source = "DefaultCover.png",
                    AlternateText = "Light Player logo"
                };

                binding.Children.Add(new AdaptiveText
                {
                    Text = $"Now Playing: {file.Title}",
                    HintMaxLines = 1,
                    HintStyle = AdaptiveTextStyle.Default
                });

                binding.Children.Add(new AdaptiveGroup
                {
                    Children =
                    {
                        new AdaptiveSubgroup
                        {
                            HintWeight = 1,
                            Children =
                            {
                                new AdaptiveText
                                {
                                    Text = $"{file.Album} - {file.Artist}",
                                    HintMaxLines = 2,
                                    HintStyle = AdaptiveTextStyle.CaptionSubtle
                                }
                            }
                        }
                    }
                });

                // Construct the entire notification
                var content = new ToastContent
                {
                    Visual = new ToastVisual
                    {
                        // Use our binding from above
                        BindingGeneric = binding,
                        // Set the base URI for the images, so we don't redundantly specify the entire path
                        BaseUri = new Uri("Assets/", UriKind.Relative)
                    },
                    // Include launch string so we know what to open when user clicks toast
                    Launch = "action=viewNowPlaying",
                    Duration = ToastDuration.Short,
                    Audio = new ToastAudio
                    {
                        Silent = true
                    }
                };

                try
                {
                    // Make sure all cleared before we send new toasts
                    ToastNotificationManager.History.Clear();
                    // Generate the toast notification content and pop the toast
                    var toastNotifier = ToastNotificationManager.CreateToastNotifier();
                    if (toastNotifier.Setting == NotificationSetting.Enabled)
                    {
                        var notification = new ToastNotification(content.GetXml());
                        // Only show in action center
                        notification.SuppressPopup = true;
                        // Now playing item should not be available for roaming
                        notification.NotificationMirroring = NotificationMirroring.Disabled;
                        toastNotifier.Show(notification);
                    }
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
                
            }
        }

        /// <summary>
        /// Clear toast history upon exit.
        /// </summary>
        public static void ClearHistory()
        {
            if (IsAdaptiveToastSupported())
            {
                try
                {
                    ToastNotificationManager.History.Clear();
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                }
            }
        }

        /// <summary>
        /// Return a boolean, indicates whether adaptive toast is supported.
        /// </summary>
        /// <returns>A boolean, indicates whether adaptive toast is supported.</returns>
        private static bool IsAdaptiveToastSupported()
        {
            switch (AnalyticsInfo.VersionInfo.DeviceFamily)
            {
                // Desktop and Mobile started supporting adaptive toasts in API contract 3 (Anniversary Update)
                case "Windows.Mobile":
                case "Windows.Desktop":
                    return ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 3);

                // Other device families do not support adaptive toasts
                default:
                    return false;
            }
        }
    }
}
