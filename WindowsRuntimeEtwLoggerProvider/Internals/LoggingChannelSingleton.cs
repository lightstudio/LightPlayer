// Copyright (c) Little Moe, LLC. All rights reserved.

using System;
using Windows.Foundation.Diagnostics;

namespace WindowsRuntimeEtwLoggerProvider.Internals
{
    /// <summary>
    /// Singleton pattern implementation of <see cref="LoggingChannel"/>.
    /// </summary>
    public class LoggingChannelSingleton
    {
        private const string DefaultChannelName = "LightStudio-Light-RuntimeEvents-Provider";
        private static Guid DefaultChannelGroupGuid = Guid.Parse("d9db2661-363d-4233-a752-88ed25aab96d");

        private LoggingChannel m_loggingChannel = new LoggingChannel(DefaultChannelName,
                new LoggingChannelOptions(DefaultChannelGroupGuid));

        public Guid ChannelId => m_loggingChannel.Id;
        public bool IsEnabled => m_loggingChannel.IsEnabled();
        public LoggingChannel Channel => m_loggingChannel;

        #region Singleton
        private static LoggingChannelSingleton m_instance;

        /// <summary>
        /// Get instance of <see cref="LoggingChannelSingleton"/>.
        /// </summary>
        public static LoggingChannelSingleton Instance
        {
            get
            {
                if (m_instance == null) m_instance = new LoggingChannelSingleton();
                return m_instance;
            }
        }
        #endregion
    }
}