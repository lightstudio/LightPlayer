#if !EFCORE_MIGRATION
using System;
using Windows.ApplicationModel.Core;
using Windows.Graphics.Display;
using Windows.UI.Core;
#endif

using Windows.Foundation.Metadata;

namespace Light.Managed.Database.Constant
{
    internal static class DatabaseConstants
    {
        public const string Legacyv4DbFileName = "library-v4.sqlite";
        public const string DbFileName = "library-v5.sqlite";
        public const string CacheDbFileName = "cache-v3.sqlite";
        public const string ExtensionFileName = "extensions-v3.sqlite";
        public const string DbAlbumCoverStorPath = "AlbumCoverStorage";
        public const string DbArtistCoverStorPath = "ArtistCoverStorage";

        public static uint ResizedSize { get; private set; }
        public static int FlushFileLimit = 10;

        static DatabaseConstants()
        {
            ResizedSize = 200;

#if !EFCORE_MIGRATION
            try
            {
#pragma warning disable 4014
                CoreApplication.MainView?.Dispatcher.TryRunAsync(CoreDispatcherPriority.High,
                    () =>
                    {
                        var displayInfo = DisplayInformation.GetForCurrentView();
                        ResizedSize = (uint) (1.25*(uint) displayInfo.ResolutionScale);
                    });
#pragma warning restore 4014
            }
            catch (InvalidOperationException)
            {
                // Ignore for migration scaffolding.
            }
#endif
        }
    }
}
