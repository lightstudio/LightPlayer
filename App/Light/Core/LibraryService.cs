using GalaSoft.MvvmLight.Messaging;
using Light.Common;
using Light.Managed.Library;
using Light.Managed.Settings;
using Light.Managed.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;

namespace Light.Core
{
    /// <summary>
    /// Proxy between library index worker and main application.
    /// </summary>
    /// <remarks>
    /// This class supports internal infrastructure.
    /// Generally this class should not be referenced anywhere else than global framework.
    /// </remarks>
    static class LibraryService
    {
        const string AutoTrackChangesKey = "AutoTrackChanges";

        static DispatcherTimer m_dispatchTimer;
        static StorageLibrary m_musicLibrary;
        static StorageLibraryChangeTracker m_libChangeTracker;
        static StorageLibraryChangeReader m_libChangeReader;
        static readonly ILoggerFactory m_loggerFactory;
        static readonly ILogger m_logger;

        static readonly FileIndexer m_fileIndexer;
        static bool m_isIndexing;

        const int PeriodicCheckFreq = 30;

        static LibraryService()
        {
            var scope = ApplicationServiceBase.App.GetScope();
            m_fileIndexer = scope.ServiceProvider.GetRequiredService<FileIndexer>();
            m_loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
            m_logger = m_loggerFactory.CreateLogger(nameof(LibraryService));
            m_fileIndexer.IndexChanged += FileIndexerOnIndexChanged;
        }

        public static bool IsIndexing
        {
            get { return m_isIndexing; }

            private set
            {
                if (m_isIndexing = value)
                    Messenger.Default.Send(new MessageBase(), CommonSharedStrings.IndexStartedMessageToken);
                else
                    Messenger.Default.Send(new MessageBase(), CommonSharedStrings.IndexFinishedMessageToken);
            }
        }

        public static bool IsAutoTrackingEnabled => SettingsManager.Instance.GetValue<bool>(AutoTrackChangesKey);

        private static void FileIndexerOnIndexChanged(object sender, IndexChangeArgs indexChangeArgs) =>
            Messenger.Default.Send(new GenericMessage<IndexChangeArgs>(indexChangeArgs),
                CommonSharedStrings.IndexChangedMessageToken);

        public static async Task IndexAsync(IThumbnailOperations thumbnail = null)
        {
            m_logger.LogInformation("External indexing started.");
            TelemetryHelper.LogEvent();

            // Start indexing
            await IndexInternalAsync(thumbnail);

            // Start tracking
            await StartChangeTrackingAsync();

            m_logger.LogInformation("External indexing completed.");
        }

        private static async Task DisableAutoTrackChangesAndNotifyUserAsync()
        {
            SettingsManager.Instance.SetValue(false, AutoTrackChangesKey);
            m_dispatchTimer?.Stop();
            m_dispatchTimer = null;

            try
            {
                m_libChangeTracker?.Reset();
                m_libChangeReader = null;
            }
            catch { }

            var notif = new MessageDialog(CommonSharedStrings.LibraryTrackingDisabled, CommonSharedStrings.MediaLibraryText);
            await notif.ShowAsync();
        }

        private static async Task<int> IndexInternalAsync(IThumbnailOperations thumbnail = null)
        {
            if (m_isIndexing) return -1;
            IsIndexing = true;
            TelemetryHelper.LogEvent();

            if (!await EnsureLibraryReferenceAsync())
            {
                await DisableAutoTrackChangesAndNotifyUserAsync();
            }
            Messenger.Default.Send(new MessageBase(), "IndexGettingFiles");
            var (count, exceptions) = await m_fileIndexer.ScanAsync(m_musicLibrary.Folders, thumbnail);
            IsIndexing = false;

            SendExceptions(exceptions);

            // Send toast and return
            SendToast(count);
            return count;
        }

        private static async Task<bool> EnsureLibraryReferenceAsync()
        {
            if (m_musicLibrary == null)
            {
                m_musicLibrary = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            }

            try
            {
                if (IsAutoTrackingEnabled && m_libChangeReader == null)
                {
                    m_libChangeTracker = m_musicLibrary.ChangeTracker;
                    m_libChangeTracker.Enable();

                    m_libChangeReader = m_libChangeTracker.GetChangeReader();
                }
                return true;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80080222)
            {
                return false;
            }
        }

        public static async Task StartChangeTrackingAsync(bool triggerIndexNow = false)
        {
            if (!IsAutoTrackingEnabled || m_dispatchTimer != null) return;

            TelemetryHelper.LogEvent();
            m_logger.LogInformation("Tracking is starting.");

            if (!await EnsureLibraryReferenceAsync())
            {
                await DisableAutoTrackChangesAndNotifyUserAsync();
                return;
            }

            m_dispatchTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(PeriodicCheckFreq) };
            m_dispatchTimer.Tick += OnPeriodicCheckTimerFired;
            m_dispatchTimer.Start();

            m_logger.LogInformation($"Tracking started. Check frequency is {PeriodicCheckFreq} seconds.");

            if (triggerIndexNow)
            {
                OnPeriodicCheckTimerFired(null, null);
                m_logger.LogInformation("An need-based incremental index session has been triggered as requested.");
            }
        }

        public static void StopChangeTracking()
        {
            if (m_dispatchTimer == null) return;

            TelemetryHelper.LogEvent();
            m_logger.LogInformation("Tracking is stopping.");

            // Stop and dereference timer
            m_dispatchTimer.Stop();
            m_dispatchTimer = null;

            try
            {
                // Stop and dereference tracker
                m_libChangeTracker.Reset();
                m_libChangeReader = null;
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80080222) { }

            m_logger.LogInformation("Tracking has stopped.");
        }

        private static async void OnPeriodicCheckTimerFired(object sender, object e)
        {
            if (m_isIndexing || m_libChangeReader == null) return;

            TelemetryHelper.LogEvent();
            m_logger.LogInformation("Enter periodic check.");

            try
            {
                var isIndexingRequired = false;
                var changes = await m_libChangeReader.ReadBatchAsync();

                foreach (var change in changes)
                {
                    // For files, validate extension.
                    if (change.IsOfType(StorageItemTypes.File) && change.Path != null)
                    {
                        if (!FileIndexer.SupportedFormats.Contains(Path.GetExtension(change.Path)))
                            continue;
                    }

                    // Determine eligibility
                    switch (change.ChangeType)
                    {
                        case StorageLibraryChangeType.Created:
                        case StorageLibraryChangeType.Deleted:
                        case StorageLibraryChangeType.MovedIntoLibrary:
                        case StorageLibraryChangeType.MovedOrRenamed:
                        case StorageLibraryChangeType.MovedOutOfLibrary:
                        case StorageLibraryChangeType.ContentsChanged:
                        case StorageLibraryChangeType.ContentsReplaced:
                            isIndexingRequired = true;
                            break;
                    }

                    // Exit the loop
                    if (isIndexingRequired) break;
                }

                m_logger.LogInformation($"Periodic check result: Reindex: {isIndexingRequired}.");

                if (isIndexingRequired)
                {
                    var updatedCount = await IndexInternalAsync(new ThumbnailOperations());
                    m_logger.LogInformation($"{updatedCount} files have been added.");
                }
                await m_libChangeReader.AcceptChangesAsync();

                m_logger.LogInformation("Periodic check has been completed.");
            }
            catch (Exception ex) when ((uint)ex.HResult == 0x80080222)
            {
                await DisableAutoTrackChangesAndNotifyUserAsync();
            }
            catch (Exception ex)
            {
                // Log exceptions to ETW channel
                m_logger.LogError($"Type of {ex.GetType()} exception occurred in {ex.StackTrace} with message {ex.Message}");
            }
        }

        private static void SendToast(int updatedCount)
        {
            string toastContent;

            if (updatedCount > 0)
            {
                toastContent = string.Format(CommonSharedStrings.LibraryUpdatedContentAdded, updatedCount);
            }
            else
            {
                toastContent = CommonSharedStrings.LibraryUpdatedContentOther;
            }

            Messenger.Default.Send(new GenericMessage<(string, string)>((
                CommonSharedStrings.LibraryUpdatedTitle, toastContent)), CommonSharedStrings.InternalToastMessage);
        }

        private static void SendExceptions(List<Tuple<string, Exception>> exceptions)
        {
            if (exceptions.Count > 0)
            {
                Messenger.Default.Send(new GenericMessage<List<Tuple<string, Exception>>>(exceptions), CommonSharedStrings.ShowLibraryScanExceptions);
            }
        }
    }
}
