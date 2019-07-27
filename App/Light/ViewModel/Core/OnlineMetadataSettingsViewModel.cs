using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using GalaSoft.MvvmLight;
using Light.Common;
using Light.Managed.Online;
using Light.Managed.Tools;
using Light.Model;
using Newtonsoft.Json;

namespace Light.ViewModel.Core
{
    /// <summary>
    /// Viewmodel for dedicated metadata settings.
    /// </summary>
    public class OnlineMetadataSettingsViewModel : ViewModelBase
    {
        private ObservableCollection<CommonBannedEntity> _bannedArtists;
        private ObservableCollection<CommonBannedEntity> _bannedAlbums;
        private ObservableCollection<AppleMusicMarketEntity> _appleMusicMrktRegions;
        private int _appleMusicMrktSelectedIndex;
        private bool _isSelectionLoaded;
        private AddButtonCommand _addArtistCommand;
        private AddButtonCommand _addAlbumCommand;

        private static string _confTextCache = string.Empty;

        /// <summary>
        /// An observable collection of all banned artists.
        /// </summary>
        public ObservableCollection<CommonBannedEntity> BannedArtists
        {
            get { return _bannedArtists; }
            set
            {
                _bannedArtists = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// An observable collection of all banned albums.
        /// </summary>
        public ObservableCollection<CommonBannedEntity> BannedAlbums
        {
            get { return _bannedAlbums; }
            set
            {
                _bannedAlbums = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// An observable collection of all Apple market regions.
        /// </summary>
        public ObservableCollection<AppleMusicMarketEntity> AppleMusicMrktRegions
        {
            get { return _appleMusicMrktRegions; }
            set
            {
                _appleMusicMrktRegions = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Indicates the index of the selected market entity.
        /// </summary>
        public int AppleMusicMrktSelectedIndex
        {
            get { return _appleMusicMrktSelectedIndex; }
            set
            {
                _appleMusicMrktSelectedIndex = value;
                RaisePropertyChanged();

                if ((0 <= _appleMusicMrktSelectedIndex && _appleMusicMrktSelectedIndex < AppleMusicMrktRegions.Count) && IsSelectionLoaded)
                {
                    var entity = AppleMusicMrktRegions[_appleMusicMrktSelectedIndex];
                    if (entity != null)
                    {
                        AggreatedOnlineMetadata.AppleMusicProviderMkrt = entity.Market;
                    }
                }
            }
        }

        /// <summary>
        /// An value indicates whether online metadata acquisition is enabled.
        /// </summary>
        public bool IsEnabled
        {
            get
            {
                return AggreatedOnlineMetadata.IsEnabled;
            }
            set
            {
                AggreatedOnlineMetadata.IsEnabled = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates whether the metadata retrieval can use metered connection.
        /// </summary>
        public bool EnableUnderMeteredNetwork
        {
            get
            {
                return AggreatedOnlineMetadata.EnableUnderMeteredNetwork;
            }
            set
            {
                AggreatedOnlineMetadata.EnableUnderMeteredNetwork = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// An value indicates whether all market selections have been loaded.
        /// </summary>
        public bool IsSelectionLoaded
        {
            get { return _isSelectionLoaded; }
            set
            {
                _isSelectionLoaded = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Command that handling adding excluded artists.
        /// </summary>
        public AddButtonCommand AddArtistCommand
        {
            get { return _addArtistCommand; }
            set
            {
                _addArtistCommand = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Command that handling adding excluded albums.
        /// </summary>
        public AddButtonCommand AddAlbumCommand
        {
            get { return _addAlbumCommand; }
            set
            {
                _addAlbumCommand = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public OnlineMetadataSettingsViewModel()
        {
            // Two lists should got initialized.
            BannedAlbums = new ObservableCollection<CommonBannedEntity>(
                Banlist.AlbumMetadataRetrieveBanlist.Select(i =>
                new CommonBannedEntity(CommonItemType.Album, i, this)));

            BannedArtists = new ObservableCollection<CommonBannedEntity>(
                Banlist.ArtistMetadataRetrieveBanlist.Select(i =>
                new CommonBannedEntity(CommonItemType.Artist, i, this)));

            AddArtistCommand = new AddButtonCommand(CommonItemType.Artist, this);
            AddAlbumCommand = new AddButtonCommand(CommonItemType.Album, this);

            AppleMusicMrktSelectedIndex = -1;
            IsSelectionLoaded = false;
            AppleMusicMrktRegions = new ObservableCollection<AppleMusicMarketEntity>();
        }

        /// <summary>
        /// Load Apple Music settings.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task LoadSettingsDataAsync()
        {
            IsSelectionLoaded = false;
            if (string.IsNullOrEmpty(_confTextCache))
            {
                try
                {
                    var file =
                        await Package.Current.InstalledLocation.GetFileAsync(CommonSharedStrings.AppleMusicMarketConfigurationFileName);
                    using (var stream = await file.OpenStreamForReadAsync())
                    using (var streamReader = new StreamReader(stream))
                        _confTextCache = await streamReader.ReadToEndAsync();
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                    return;
                }
            }

            var marketCollection = JsonConvert.DeserializeObject<List<AppleMusicMarketEntity>>(_confTextCache);
            var marketIndex = 0;
            var currentMarketIndex = 0;

            foreach (var market in marketCollection)
            {
                AppleMusicMrktRegions.Add(market);

                if (AggreatedOnlineMetadata.AppleMusicProviderMkrt == market.Market)
                {
                    currentMarketIndex = marketIndex;
                }
                marketIndex++;
            }

            AppleMusicMrktSelectedIndex = currentMarketIndex;
            IsSelectionLoaded = true;
        }

        /// <summary>
        /// Cleanup method.
        /// </summary>
        public override void Cleanup()
        {
            BannedAlbums.Clear();
            BannedArtists.Clear();
            AppleMusicMrktRegions.Clear();
            base.Cleanup();
        }

        /// <summary>
        /// Common add button command.
        /// </summary>
        public class AddButtonCommand : ICommand
        {
            private readonly OnlineMetadataSettingsViewModel m_baseViewModel;
            private int m_executionCount;

            public CommonItemType Type { get; }

            public AddButtonCommand(CommonItemType type, OnlineMetadataSettingsViewModel baseViewModelViewModel)
            {
                Type = type;
                m_baseViewModel = baseViewModelViewModel;
                m_executionCount = 0;
            }

            public bool CanExecute(object parameter)
            {
                if (m_executionCount > 0) return false;

                if (parameter is string)
                {
                    return !string.IsNullOrEmpty((string)parameter);
                }

                return false;
            }

            public void Execute(object parameter)
            {
                // Determine the current condition
                if (parameter is string)
                {
                    Interlocked.Increment(ref m_executionCount);
                    CanExecuteChanged?.Invoke(this, new EventArgs());

                    try
                    {
                        var param = (string)parameter;
                        var entity = new CommonBannedEntity(Type, param, m_baseViewModel);
                        switch (Type)
                        {
                            case CommonItemType.Album:
                                Banlist.AlbumMetadataRetrieveBanlist.Add(param);
                                m_baseViewModel.BannedAlbums.Add(entity);
                                break;
                            case CommonItemType.Artist:
                                Banlist.ArtistMetadataRetrieveBanlist.Add(param);
                                m_baseViewModel.BannedArtists.Add(entity);
                                break;
                        }
                    }
                    finally
                    {
                        Interlocked.Decrement(ref m_executionCount);
                        CanExecuteChanged?.Invoke(this, new EventArgs());
                    }
                }
            }

#pragma warning disable 67 // Reserved for XAML Framework
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67
        }
    }

    /// <summary>
    /// Common banned entity.
    /// </summary>
    public class CommonBannedEntity
    {
        public CommonItemType Type { get; }
        public string Name { get; }
        public DeleteCommand Delete { get; }

        /// <summary>
        /// Class constructor for banned entity.
        /// </summary>
        /// <param name="type">Item entity.</param>
        /// <param name="name">Item name.</param>
        /// <param name="parentViewModel">Parental view model.</param>
        public CommonBannedEntity(CommonItemType type, string name, OnlineMetadataSettingsViewModel parentViewModel)
        {
            Type = type;
            Name = name;
            Delete = new DeleteCommand(parentViewModel, this);
        }

        public class DeleteCommand : ICommand
        {
            private readonly OnlineMetadataSettingsViewModel m_parentViewModel;
            private readonly CommonBannedEntity m_entity;
            private int m_executionCount;

            public DeleteCommand(OnlineMetadataSettingsViewModel parentViewModel, CommonBannedEntity entity)
            {
                m_parentViewModel = parentViewModel;
                m_entity = entity;
                m_executionCount = 0;
            }

            public bool CanExecute(object parameter) => m_executionCount < 1;

            public void Execute(object parameter)
            {
                Interlocked.Increment(ref m_executionCount);
                CanExecuteChanged?.Invoke(this, new EventArgs());

                try
                {
                    switch (m_entity.Type)
                    {
                        case CommonItemType.Album:
                            Banlist.AlbumMetadataRetrieveBanlist.Remove(m_entity.Name);
                            m_parentViewModel.BannedAlbums.Remove(m_entity);
                            break;
                        case CommonItemType.Artist:
                            Banlist.ArtistMetadataRetrieveBanlist.Remove(m_entity.Name);
                            m_parentViewModel.BannedArtists.Remove(m_entity);
                            break;
                    }
                }
                finally
                {
                    Interlocked.Decrement(ref m_executionCount);
                    CanExecuteChanged?.Invoke(this, new EventArgs());
                }
            }

#pragma warning disable 67 // Reserved for XAML Framework
            public event EventHandler CanExecuteChanged;
#pragma warning restore 67
        }
    }

    /// <summary>
    /// Apple Music market entity.
    /// </summary>
    public class AppleMusicMarketEntity
    {
        private static readonly ResourceLoader ResLoader;

        static AppleMusicMarketEntity()
        {
            ResLoader = ResourceLoader.GetForViewIndependentUse();
        }

        /// <summary>
        /// Actual market ID.
        /// </summary>
        [JsonProperty("market")]
        public string Market { get; set; }

        /// <summary>
        /// Market resource tag.
        /// </summary>
        [JsonProperty("resTag")]
        public string ResTag { get; set; }

        /// <summary>
        /// The displayed market on UI.
        /// </summary>
        public string DisplayMarket => ResLoader.GetString(ResTag ?? "AppleMusicMrktenUS");
    }
}
