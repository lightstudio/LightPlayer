// Copyright (c) Little Moe, LLC. All rights reserved.

using System;

namespace Microsoft.Extensions.Logging
{
    public static class WindowsRuntimeEtwLoggerFactoryExtensions
    {
        /// <summary>
        /// Adds a ETW channel logger that is enabled for <see cref="LogLevel"/>.Information or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        public static ILoggerFactory AddEtwChannel(this ILoggerFactory factory)
        {
            return AddEtwChannel(factory, LogLevel.Information);
        }

        /// <summary>
        /// Adds a ETW channel logger that is enabled as defined by the filter function.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="filter">The function used to filter events based on the log level.</param>
        public static ILoggerFactory AddEtwChannel(this ILoggerFactory factory, Func<string, LogLevel, bool> filter)
        {
            factory.AddProvider(new WindowsRuntimeEtwLoggerProvider.WindowsRuntimeEtwLoggerProvider(filter));
            return factory;
        }

        /// <summary>
        /// Adds a ETW channel logger that is enabled for <see cref="LogLevel"/>s of minLevel or higher.
        /// </summary>
        /// <param name="factory">The extension method argument.</param>
        /// <param name="minLevel">The minimum <see cref="LogLevel"/> to be logged</param>
        public static ILoggerFactory AddEtwChannel(this ILoggerFactory factory, LogLevel minLevel)
        {
            return AddEtwChannel(
               factory,
               (_, logLevel) => logLevel >= minLevel);
        }

    }
}
