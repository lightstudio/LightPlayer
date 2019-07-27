// Copyright (c) Little Moe, LLC. All rights reserved.

using Microsoft.Extensions.Logging;
using System;
using Windows.Foundation.Diagnostics;
using WindowsRuntimeEtwLoggerProvider.Internals;

namespace WindowsRuntimeEtwLoggerProvider
{
    /// <summary>
    /// A logger that writes messages in the Windows ETW message channel.
    /// </summary>
    public class WindowsRuntimeEtwLogger : ILogger
    {

        private readonly string m_name;
        private readonly Func<string, LogLevel, bool> m_filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsRuntimeEtwLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        public WindowsRuntimeEtwLogger(string name) : this(name, filter: null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsRuntimeEtwLogger"/> class.
        /// </summary>
        /// <param name="name">The name of the logger.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public WindowsRuntimeEtwLogger(string name, Func<string, LogLevel, bool> filter)
        {
            m_name = string.IsNullOrEmpty(name) ? nameof(WindowsRuntimeEtwLogger) : name;
            m_filter = filter;
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        /// <inheritdoc />
        public bool IsEnabled(LogLevel logLevel)
        {
            // If the filter is null, everything is enabled
            // unless the channel is not enabled
            return LoggingChannelSingleton.Instance.IsEnabled &&
                (m_filter == null || m_filter(m_name, logLevel));
        }

        /// <inheritdoc />
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message)) return;

            LoggingChannelSingleton.Instance.Channel.LogMessage(message, ConvertRuntimeLoggingLevel(logLevel));
        }

        private static Func<LogLevel, LoggingLevel> ConvertRuntimeLoggingLevel = logLevel =>
        {
            switch(logLevel)
            {
                case LogLevel.Critical:
                    return LoggingLevel.Critical;
                case LogLevel.Error:
                    return LoggingLevel.Error;
                case LogLevel.Warning:
                    return LoggingLevel.Warning;
                case LogLevel.Information:
                    return LoggingLevel.Information;
                case LogLevel.Debug:
                    return LoggingLevel.Verbose;
                default:
                    return LoggingLevel.Verbose;
            }
        };

        private class NoopDisposable : IDisposable
        {

            public static NoopDisposable Instance = new NoopDisposable();

            public void Dispose()
            {
                // Do nothing
            }
        }

    }
}
