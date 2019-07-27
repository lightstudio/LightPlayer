using Light.Managed.Settings;
using Microsoft.ApplicationInsights;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using WindowsRuntimeEtwLoggerProvider.Internals;

namespace Light.Managed.Tools
{
    /// <summary>
    /// Application Insight Telemetry helper class.
    /// </summary>
    public static class TelemetryHelper
    {
        private static readonly TelemetryClient TelemetryClient = new TelemetryClient();
        private static bool _isInitialized;
        private const string TraceIdMessage = "See traced event";
        private const string ExceptionEvent = "Light-Unhandled-Exceptions";

        public static bool OptinTelemetry => SettingsManager.Instance.GetValue<bool>();

        /// <summary>
        /// Should be called __FIRST__ - Initialize telemetry client.
        /// </summary>
        /// <returns></returns>
        public static async void Initialize()
        {
            await InitializeInternalAsync();
        }

        /// <summary>
        /// Internal method for Telemetry initialization.
        /// </summary>
        private static async Task InitializeInternalAsync()
        {
            if (!OptinTelemetry) return;
            await WindowsAppInitializer.InitializeAsync();
            _isInitialized = true;
        }

        /// <summary>
        /// Track an exception.
        /// </summary>
        /// <param name="ex">Instance of <see cref="Exception"/></param>
        public static async void TrackExceptionAsync(Exception ex)
        {
            if (!OptinTelemetry || ex == null) return;
            if (!_isInitialized) await InitializeInternalAsync();
            TelemetryClient.TrackException(ex);

            try
            {
                var lgFields = new LoggingFields();
                lgFields.AddString("Message", ex.Message ?? string.Empty, LoggingFieldFormat.String);
                lgFields.AddString("StackTrace", ex.StackTrace ?? string.Empty, LoggingFieldFormat.String);
                lgFields.AddString("Detailed", ex.ToString(), LoggingFieldFormat.String);

                LogEventWithParams(ExceptionEvent, lgFields, LoggingLevel.Error);
            }
            catch
            {
                // Ignore
            }
        }

        /// <summary>
        /// Log serialized exception.
        /// </summary>
        /// <param name="strException">Serialized exception.</param>
        public static void LogSerializedException(string strException)
        {
            if (strException == null) return;

            try
            {
                var lgFields = new LoggingFields();
                lgFields.AddString("Detailed", strException, LoggingFieldFormat.String);
                LogEventWithParams(ExceptionEvent, lgFields, LoggingLevel.Error);
            }
            catch
            {
                // Ignore
            }
        }

        /// <summary>
        /// Track event.
        /// </summary>
        /// <param name="eventTitle"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        public static async Task TraceEventAsync(string eventTitle, IDictionary<string, string> properties = null)
        {
            if (!OptinTelemetry) return;
            if (!_isInitialized) await InitializeInternalAsync();
            TelemetryClient.TrackEvent(eventTitle, properties);
        }

        /// <summary>
        /// Unique user ID for analsysis.
        /// </summary>
        public static string UserId
        {
            get
            {
                if (!OptinTelemetry)
                {
                    return string.Empty;
                }
                else
                {
                    return TelemetryClient.Context?.User?.Id ?? TraceIdMessage;
                }
            }
        }

        /// <summary>
        /// Unique device ID for analysis.
        /// </summary>
        public static string DeviceId
        {
            get
            {
                if (!OptinTelemetry)
                {
                    return string.Empty;
                }
                else
                {
                    return TelemetryClient.Context?.Device?.Id ?? TraceIdMessage;
                }
            }
        }

        /// <summary>
        /// Instrumentation ID for analysis.
        /// </summary>
        public static string InstrumentationId
        {
            get
            {
                if (!OptinTelemetry)
                {
                    return string.Empty;
                }
                else
                {
                    return TraceIdMessage;
                }
            }
        }

        /// <summary>
        /// Log event to ETW logger.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        public static void LogEvent([CallerMemberName] string eventName = "Light-Unknown-Event")
        {
            LoggingChannelSingleton.Instance.Channel.LogEvent(eventName);
        }

        /// <summary>
        /// Log event to ETW logger with additional fields.
        /// </summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="fields">Additional event fields.</param>
        /// <param name="level">Logging level. Default level is verbose.</param>
        public static void LogEventWithParams(string eventName, 
            LoggingFields fields, LoggingLevel level = LoggingLevel.Verbose)
        {
            LoggingChannelSingleton.Instance.Channel.LogEvent(eventName, fields, level);
        }
    }
}
