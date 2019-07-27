// Copyright (c) Little Moe, LLC. All rights reserved.

using Microsoft.Extensions.Logging;
using System;

namespace WindowsRuntimeEtwLoggerProvider
{
    /// <summary>
    /// The provider for the <see cref="WindowsRuntimeEtwLogger"/>.
    /// </summary>
    public class WindowsRuntimeEtwLoggerProvider : ILoggerProvider
    {

        private readonly Func<string, LogLevel, bool> m_filter;

        /// <summary>
        /// Initializes a new instance of the <see cref="WindowsRuntimeEtwLoggerProvider"/> class.
        /// </summary>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public WindowsRuntimeEtwLoggerProvider(Func<string, LogLevel, bool> filter)
        {
            m_filter = filter;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new WindowsRuntimeEtwLogger(categoryName, m_filter);
        }

        public void Dispose()
        {
            // Do nothing
        }

    }
}
