using System;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;

namespace Light.Common
{
    /// <summary>
    /// One place for all shared strings.
    /// </summary>
    public static class CommonSharedStrings
    {
        /// <summary>
        /// Resource loader.
        /// </summary>
        private static readonly ResourceLoader Loader = ResourceLoader.GetForCurrentView();

        // PlaybackButtonCommands
        public const string PlayOrPause = "PlayOrPause";
        public const string Next = "Next";
        public const string Prev = "Prev";

        // ExceptionHelper
        public static string DeviceNotSupportedString = GetString();

        // Platform
        public const string WindowsDesktopId = "Windows.Desktop";
        public const string WindowsMobileId = "Windows.Mobile";
        public const string WindowsTeamId = "Windows.Team";
        public const string WindowsIoTId = "Windows.IoT";
        public const string WindowsHolographicId = "Windows.Holographic";

        // Dual factor preview behavior
        public const string DefaultAlbumImagePath = "ms-appx:///Assets/DefaultCover.png";
        public const string DefaultArtistImagePath = "ms-appx:///Assets/DefaultArtist.png";

        // Grouping option template (Views)
        // {0} = Scenario
        public const string GroupOptionTemplate = "{0}.GP";

        // IAP
        public static string IapPurchaseSucceededPrompt = GetString();
        public static string IapPurchaseSucceededTitle = GetString();
        public static string IapBuildNotSupportedErrorPrompt = GetString();
        public static string IapBuildNotSupportedErrorTitle = GetString();

        // Settings
        public const string JavaScriptFileFormatSuffix = ".js";
        public const string OssLicenseFileName = "OSSLICENSE.txt";

        // Detailed page header control view model
        public const string ControlPageShareClickedEventMessageToken = "ControlPageShareClicked";

        // Detailed page header control actions
        public const string Share = "Share";
        public const string Play = "Play";
        public const string AddToNowPlaying = "AddToNowPlaying";

        // ImagePreview - Local covers
        public const string FolderJpg = "Folder.jpg";
        public const string FolderPng = "Folder.png";
        public const string CoverJpg = "Cover.jpg";
        public const string CoverPng = "Cover.png";

        // MediaPlaybackItemIndicator
        public const string PlayingTextGlyph = "\uE768";
        public const string PausedTextGlyph = "\uE769";

        // PlaybackStatusToIconConverter
        public const string PauseTextGlyph = "\uE103";
        public const string ChangeTextGlyph = "\uE10C";

        // Index Service
        public const string IndexStartedMessageToken = "IndexStarted";
        public const string IndexFinishedMessageToken = "IndexFinished";
        public const string IndexChangedMessageToken = "IndexChanged";
        public const string IndexItemAddedMessageToken = "IndexItemAdded";
        public const string IndexGettingFilesMessageToken = "IndexGettingFiles";

        public const string ContentFrameNavigateToken = "ContentFrameNavigate";

        // GlobalExceptionHandler
        public const string CrashLogFilenameTemplate =
            "CrashLog - {0}.log";
        public static string CrashLogFileTemplate
            = $"Unhandled Exception{Environment.NewLine}Please send this log file to light@ligstd.com{Environment.NewLine}------ BEGIN APPLICATION CRASH LOG ------{Environment.NewLine}{{0}}";
        public const string UnknownErrorMessageTitle = "Something happened";
        public static string UnknownErrorPromptContent = $"An exception is thrown:{Environment.NewLine} {{0}}";
        public static string InnerExceptionPromptContent = $"Inner Exception:{Environment.NewLine} {{0}}{Environment.NewLine}{{1}}{Environment.NewLine}{{2}}";
        public const string SuppressUnknownErrorPromptText = "Don't show this again.";
        public const string UnknownErrorPromptMainButtonText = "OK";

        // DeleteConfirmation
        public static string DeleteConfirmationText = GetString();

        // Lyric Flyout
        public static string ManualSelectLyricButtonText = GetString();
        public const string TxtFileSuffix = ".txt";
        public const string LrcFileSuffix = ".lrc";
        public static string SearchError = GetString();
        public static string LrcNotFound = GetString();
        public static string LrcDownloadFailed = GetString();

        public const string JpgFileSuffix = ".jpg";
        public const string PngFileSuffix = ".png";

        // CommonViewItem
        public static string AlbumSubtitleFormat = GetString();
        public static string ArtistSubtitleFormat = GetString();
        public static string AlbumSubtitleFallbackFormat = GetString();
        public static string ArtistSubtitleFallbackFormat = GetString();
        public static string ArtistSubtitleFallbackFormat2 = GetString();
        public static string SongSubtitleFormat = GetString();
        public static string UnknownAlbumTitle = GetString();
        public static string UnknownArtistTitle = GetString();
        public static string PlaylistSubtitle = GetString();
        public static string PlaylistSingleItemSubtitle = GetString();
        public static string PlaylistNoItemSubtitle = GetString();
        public static string PlaylisFallbackSubtitle = GetString();
        public static string DefaultFileName = GetString();
        public static string DefaultAlbumName = GetString();
        public static string DefaultArtistName = GetString();

        // Jumplist
        public const string JumplistAlbumIconPath = "ms-appx:///Assets/Jumplist/Album.png";
        public const string JumplistArtistIconPath = "ms-appx:///Assets/Jumplist/Artist.png";
        public const string JumplistSongIconPath = "ms-appx:///Assets/Jumplist/Song.png";
        public const string JumplistPlaylistIconPath = "ms-appx:///Assets/Jumplist/Playlist.png";
        public static string CategoryGroupName = GetString();
        public static string JumplistAlbumText = GetString();
        public static string JumplistArtistText = GetString();
        public static string JumplistSongText = GetString();
        public static string JumplistPlaylistText = GetString();
        public const string JumplistAlbumUrl = "light-jumplist:viewallalbums";
        public const string JumplistArtistUrl = "light-jumplist:viewallartists";
        public const string JumplistSongUrl = "light-jumplist:viewallsongs";
        public const string JumplistPlaylistUrl = "light-jumplist:viewallplaylist";

        // DateTime
        public static string UnknownDate = GetString();

        // LiveTile
        public static string LiveTileLine3 = GetString();

        // FrameView
        public const string FrameViewNavigationMessageToken = "FrameViewNavigationRequestMessage";
        public const string FrameViewNavigationIntMessageToken = "FrameViewNavigationRequestMessageInt";
        public const string InnerSplitViewModeChangeToken = "InnerSplitViewModeChange";
        public const string ShowNowPlayingViewToken = "ShowNowPlayingView";

        // FrameView Player
        public static string DefaultPlaylistTempalte = GetString();
        public static string PlaylistSaveErrorTitle = GetString();
        public static string PlaylisySaveSucceededTitle = GetString();

        // LibraryHostView
        public const string HostedPageNavigationMessage = "HostedPageNavigationMessage";
        public const string SelectedLibraryViewType = "SelectedLibraryViewTyp";
        public static string LibraryTitle = GetString();

        // Settings
        public static string SettingsTitle = GetString();

        // Extended Splashscreen
        public static string ProvisionPrompt = GetString();

        // Item command
        public const string AddToPlaylist = "AddToPlaylist";
        public const string PlayAsNext = "PlayAsNext";

        // Property view
        public static string PropertyTitle = GetString();

        // Search
        public static string SearchTitleTemplate = GetString();

        // Sorting
        public const string UnknownIndex = "...";

        // Grouping change event token
        public const string GroupingChangeMessageToken = "ViewGroupingChangeMessage";
        public const string GroupingHostPageSelectionChangeMessageToken = "GroupingHostPageSelectionChangeMessage";

        // Now playing page
        public static string NowPlayingTitle = GetString();
        public static string NowPlayingEmptyTitle = GetString();

        // Lyrics manual select
        public static string NoResultText = GetString();
        public static string OneResultText = GetString();
        public static string MultipleResultsText = GetString();

        public static string FavoritesLocalizedText = GetString();

        public static string NewNameEmptyPrompt = GetString();
        public static string CannotRenameFavoritePrompt = GetString();
        public static string PlaylistAlreadyExistPrompt = GetString();
        public static string RenameString = GetString();
        public static string NewPlaylistString = GetString();
        public static string PlaylistDefaultname = GetString();
        public static string ConfirmString = GetString();
        public static string CancelString = GetString();
        public static string ClearString = GetString();
        public static string DeleteString = GetString();
        public static string PlaylistDeleteTitle = GetString();
        public static string PlaylistDeleteDescription = GetString();
        public static string FavoriteDeleteDescription = GetString();
        public static string FavoriteClearTitle = GetString();

        public static string InternalErrorTitle = GetString();
        public static string FileSizeLimitPrompt = GetString();
        public static string NoValidThumbnailPrompt = GetString();
        public static string DownloadFailedPrompt = GetString();

        public static string PlaylistSaved = GetString();
        public static string FailedToSavePlaylist = GetString();
        public static string M3u = GetString();
        public static string ChooseFileName = GetString();
        public static string ValidFilenameRequired = GetString();
        public static string AlreadyHaveAccessPrompt = GetString();
        public static string AccessAuthorizeSuggestDetailed = GetString();
        public static string SelectionNotInLibraryPrompt = GetString();
        public static string NowPlayingUpper = GetString();
        public static string ContinuePlaylistUpper = GetString();
        public static string MyMusicUpper = GetString();
        public static string Home = GetString();
        public static string ToolTipPause = GetString();
        public static string ToolTipPlay = GetString();
        public static string Error = GetString();
        public static string FileNotFound = GetString();
        public static string Select = GetString();

        public static string TitleText = GetString();
        public static string AlbumText = GetString();
        public static string AlbumArtistText = GetString();
        public static string ArtistText = GetString();
        public static string Composer = GetString();
        public static string Date = GetString();
        public static string Track = GetString();
        public static string Disc = GetString();
        public static string TotalDiscs = GetString();
        public static string TotalTracks = GetString();
        public static string GenreText = GetString();
        public static string Performer = GetString();
        public static string Grouping = GetString();
        public static string Comment = GetString();
        public static string CopyrightText = GetString();
        public static string Description = GetString();
        public static string Time = GetString();
        // New tags
        public static string SortedAlbumText = GetString();
        public static string SortedArtistText = GetString();
        public static string EncoderText = GetString();
        public static string PublisherText = GetString();
        public static string SortedTitleText = GetString();
        public static string TsrcText = GetString();

        public const string AppleMusicMarketConfigurationFileName = "AppleMusicMarketConfiguration.json";

        public const string StoreReleaseKey = "Store Release";
        public const string SideLoadKey = "Local Instllation";

        public const string Archx86 = nameof(Archx86);
        public const string Archx64 = nameof(Archx64);
        public const string ArchARM = nameof(ArchARM);
        public const string ArchARM64 = nameof(ArchARM64);
        public const string ArchNetural = nameof(ArchNetural);
        public const string ArchUnknown = nameof(ArchUnknown);

        // Internal toast
        public const string InternalToastMessage = "FrameInternalToast";

        // Library toast
        public static string LibraryUpdatedTitle = GetString();
        public static string LibraryUpdatedContentAdded = GetString();
        public static string LibraryUpdatedContentOther = GetString();

        public const string ShowLibraryScanExceptions = "ShowLibraryScanExceptions";

        public static string SingleException = GetString();
        public static string MultipleExceptionsFormat = GetString();
        public static string LibraryFilesAddedFormat = GetString();

        public static string Failure = GetString();
        public static string PlaybackErrorFormat = GetString();

        public static string MediaLibraryScanWarningText = GetString();
        public static string Warning = GetString();

        public static string Music = GetString();

        public static string LibraryTrackingDisabled = GetString();
        public static string MediaLibraryText = GetString();

        /// <summary>
        /// New universal methods for all strings.
        /// </summary>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static string GetString([CallerMemberName]string tag = null) => Loader.GetString(tag);
    }
}
