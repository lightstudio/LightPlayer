using System;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Notifications;
using Light.Common;
using Light.Managed.Database.Entities;
using Light.Managed.Tools;
using XmlElement = Windows.Data.Xml.Dom.XmlElement;

namespace Light.Utilities
{
    static class LiveTileHelper
    {
        private static readonly object _lock = new object();
        public static void UpdateMainTileForPlayback(DbMediaFile item, string imageUrl)
        {
            lock (_lock)
            {
                // Not supported on IoT and Holographic platform.
                if (PlatformInfo.CurrentPlatform != Platform.WindowsMobile &&
                    PlatformInfo.CurrentPlatform != Platform.WindowsDesktop)
                    return;

                // Large tile
                var largeTileXml =
                    TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare310x310ImageAndTextOverlay02);
                var largeTileTextAttributes = largeTileXml.GetElementsByTagName("text");
                largeTileTextAttributes[0].InnerText = item.Title;
                largeTileTextAttributes[1].InnerText = $"{item.Album} by {item.Artist}";
                var largeTileImageAttributes = largeTileXml.GetElementsByTagName("image");
                ((XmlElement)largeTileImageAttributes[0]).SetAttribute("src", string.IsNullOrEmpty(imageUrl) ? 
                    CommonSharedStrings.DefaultAlbumImagePath : imageUrl);
                ((XmlElement)largeTileImageAttributes[0]).SetAttribute("alt", item.Album);

                // Wide tile
                var tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150SmallImageAndText02);
                var tileTextAttributes = tileXml.GetElementsByTagName("text");
                tileTextAttributes[0].InnerText = item.Title;
                tileTextAttributes[1].InnerText = item.Album;
                tileTextAttributes[2].InnerText = item.Artist;
                tileTextAttributes[3].InnerText = CommonSharedStrings.LiveTileLine3;
                var tileImageAttributes = tileXml.GetElementsByTagName("image");
                ((XmlElement)largeTileImageAttributes[0]).SetAttribute("src", string.IsNullOrEmpty(imageUrl) ?
                    CommonSharedStrings.DefaultAlbumImagePath : imageUrl);
                ((XmlElement)tileImageAttributes[0]).SetAttribute("alt", item.Album);

                // Square tile
                var sqTileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150PeekImageAndText02);
                var sqTileTextAttributes = sqTileXml.GetElementsByTagName("text");
                sqTileTextAttributes[0].InnerText = item.Title;
                sqTileTextAttributes[1].InnerText = $"{item.Album} by {item.Artist}";
                var sqTileImageAttributes = sqTileXml.GetElementsByTagName("image");
                ((XmlElement)largeTileImageAttributes[0]).SetAttribute("src", string.IsNullOrEmpty(imageUrl) ?
                    CommonSharedStrings.DefaultAlbumImagePath : imageUrl);
                ((XmlElement)sqTileImageAttributes[0]).SetAttribute("alt", item.Album);

                // Merge
                var node = largeTileXml.ImportNode(tileXml.GetElementsByTagName("binding").Item(0), true);
                // ReSharper disable once PossibleNullReferenceException
                largeTileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);
                node = largeTileXml.ImportNode(sqTileXml.GetElementsByTagName("binding").Item(0), true);
                // ReSharper disable once PossibleNullReferenceException
                largeTileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);

                var tileNotification = new TileNotification(largeTileXml) { ExpirationTime = DateTimeOffset.UtcNow.AddHours(2.0) };

                TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);
            }
        }

        public static async void CleanUpCacheAsync()
        {
            try
            {
                var fileQuery = ApplicationData.Current.LocalFolder.CreateFileQuery(CommonFileQuery.DefaultQuery);
                fileQuery.ApplyNewQueryOptions(new QueryOptions
                {
                    FileTypeFilter = {".png"},
                    FolderDepth = FolderDepth.Shallow
                });
                var files = await fileQuery.GetFilesAsync();
                foreach (var f in files)
                {
                    try
                    {
                        await f.DeleteAsync();
                    }
                    catch (Exception ex)
                    {
                        TelemetryHelper.TrackExceptionAsync(ex);
                    }
                }
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }
    }
}
