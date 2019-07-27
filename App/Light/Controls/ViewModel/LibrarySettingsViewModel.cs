using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Light.Common;
using Light.Controls.Models;
using Light.Managed.Settings;
using Light.Managed.Tools;
using RelayCommand = GalaSoft.MvvmLight.Command.RelayCommand;
using Light.Utilities;
using Windows.Storage.Pickers;
using Windows.UI.Popups;
using Light.Core;
using System.IO;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// Library settings sub-viewmodel for common settings viewmodel.
    /// </summary>
    public class LibrarySettingsViewModel : ViewModelBase
    {
        const string LibraryRefreshKey = "AutoMedialibraryRefresh";
        const string AutoTrackChangesKey = "AutoTrackChanges";

        private bool _isLibraryOpsAvailable;
        private bool _isAutoMedialibraryRefreshEnabled;
        private bool _enablePlaybackHistory;
        private bool _isTrackLibraryChangesEnabled;
        private ObservableCollection<FolderModel> _libaryFolders;
        private ObservableCollection<FolderModel> _excludedFolders;
        private readonly RelayCommand<RoutedEventArgs> _removeFolderButtonClickedRelayCommand;

        /// <summary>
        /// Handler for adding folder button.
        /// </summary>
        public RelayCommand AddFolderRelayCommand { get; }

        public RelayCommand AddExcludedRelayCommand { get; }

        public RelayCommand AddAccessFolderRelayCommand { get; }

        /// <summary>
        /// Indicates whether auto media library refresh is enabled.
        /// </summary>
        public bool IsAutoMedialibraryRefreshEnabled
        {
            get { return _isAutoMedialibraryRefreshEnabled; }
            set
            {
                _isAutoMedialibraryRefreshEnabled = value;
                RaisePropertyChanged();
                SettingsManager.Instance.SetValue(value, LibraryRefreshKey);
            }
        }

        /// <summary>
        /// Indicates whether library change tracking is enabled.
        /// </summary>
        public bool IsTrackLibraryChangesEnabled
        {
            get { return _isTrackLibraryChangesEnabled; }
            set
            {
                _isTrackLibraryChangesEnabled = value;
                RaisePropertyChanged();
                SettingsManager.Instance.SetValue(value, AutoTrackChangesKey);
            }
        }

        public bool EnablePlaybackHistory
        {
            get
            {
                return _enablePlaybackHistory;
            }
            set
            {
                _enablePlaybackHistory = value;
                RaisePropertyChanged();
                SettingsManager.Instance.SetValue(value, nameof(EnablePlaybackHistory));
            }
        }

        /// <summary>
        /// Indicates whether library operation is available on the device.
        /// </summary>
        public bool IsLibraryOpsAvailable
        {
            get { return _isLibraryOpsAvailable; }
            set
            {
                _isLibraryOpsAvailable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A collection of library folders.
        /// </summary>
        public ObservableCollection<FolderModel> LibraryFolders
        {
            get { return _libaryFolders; }
            set { Set(ref _libaryFolders, value); }
        }

        public ObservableCollection<FolderModel> ExcludedFolders
        {
            get { return _excludedFolders; }
            set { Set(ref _excludedFolders, value); }
        }

        public ObservableCollection<FolderModel> AccessFolders { get; } = new ObservableCollection<FolderModel>();
        /// <summary>
        /// Class constructor.
        /// </summary>
        public LibrarySettingsViewModel()
        {
            AddFolderRelayCommand = new RelayCommand(AddFolderStub);
            AddAccessFolderRelayCommand = new RelayCommand(AddAccessFolder);
            AddExcludedRelayCommand = new RelayCommand(AddExcluded);
            LibraryFolders = new ObservableCollection<FolderModel>();
            ExcludedFolders = new ObservableCollection<FolderModel>();
            _removeFolderButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(OnRemoveFolderButtonClicked);

            // Device family specific logic
            IsLibraryOpsAvailable = PlatformInfo.CurrentPlatform == Platform.WindowsDesktop;

            // Load settings
            _isAutoMedialibraryRefreshEnabled = SettingsManager.Instance.GetValue<bool>(LibraryRefreshKey);
            _isTrackLibraryChangesEnabled = SettingsManager.Instance.GetValue<bool>(AutoTrackChangesKey);
            _enablePlaybackHistory = SettingsManager.Instance.GetValue<bool>(nameof(EnablePlaybackHistory));
        }

        private async void AddExcluded()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.MusicLibrary,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                if (!LibraryFolders.Any(f => folder.Path.IsSubPathOf(f.Path)))
                {
                    await new MessageDialog(CommonSharedStrings.SelectionNotInLibraryPrompt).ShowAsync();
                }
                else
                {
                    var folderModel = new FolderModel
                    {
                        Path = folder.Path,
                        Name = folder.Name
                    };
                    PathExclusion.AddExcludedPath(folder.Path);
                    folderModel.RemoveFolderButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(e =>
                    {
                        PathExclusion.RemoveExcludedPath(folder.Path);
                        ExcludedFolders.Remove(folderModel);
                    });
                    ExcludedFolders.Add(folderModel);
                }
            }
        }

        private async void AddAccessFolder()
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.ComputerFolder,
                ViewMode = PickerViewMode.List
            };
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                if (AccessFolders.Any(f => folder.Path.IsSubPathOf(f.Path)) ||
                    LibraryFolders.Any(f => folder.Path.IsSubPathOf(f.Path)))
                {
                    await new MessageDialog(CommonSharedStrings.AlreadyHaveAccessPrompt).ShowAsync();
                }
                else
                {
                    var folderModel = new FolderModel
                    {
                        Path = folder.Path,
                        Name = folder.Name
                    };
                    var token = await FutureAccessListHelper.Instance.AuthorizeStorageItem(folder);
                    folderModel.RemoveFolderButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(
                        async (e) =>
                        {
                            await FutureAccessListHelper.Instance.RemoveAuthorizedItemAsync(token);
                            AccessFolders.Remove(folderModel);
                        });
                    AccessFolders.Add(folderModel);
                }
            }
        }

        /// <summary>
        /// Handle adding folders.
        /// </summary>
        private async void AddFolderStub()
        {
            // Select a folder.
            var libraryFolders = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            var folder = await libraryFolders.RequestAddFolderAsync();
            if (folder == null || LibraryFolders.Any((c) => c.Path == folder.Path)) return;
            // Add folder to library.
            LibraryFolders.Add(new FolderModel
            {
                Name = folder.Name,
                Path = folder.Path,
                RemoveFolderButtonClickedRelayCommand = _removeFolderButtonClickedRelayCommand
            });
        }

        /// <summary>
        /// Method for loading library folders.
        /// </summary>
        /// <returns></returns>
        public async Task LoadFoldersAsync()
        {
            // Library
            var libraryFolders = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
            foreach (var folder in libraryFolders.Folders)
            {
                LibraryFolders.Add(new FolderModel
                {
                    Path = folder.Path,
                    Name = folder.Name,
                    RemoveFolderButtonClickedRelayCommand = _removeFolderButtonClickedRelayCommand
                });
            }
            var excludedFolders = PathExclusion.GetExcludedPath();
            foreach (var folder in excludedFolders)
            {
                var folderModel = new FolderModel
                {
                    Path = folder,
                    Name = Path.GetDirectoryName(folder)
                };
                folderModel.RemoveFolderButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(e =>
                {
                    PathExclusion.RemoveExcludedPath(folder);
                    ExcludedFolders.Remove(folderModel);
                });
                ExcludedFolders.Add(folderModel);
            }
            var accessFolders = await FutureAccessListHelper.Instance.GetAuthroizedStorageItemsAsync();
            foreach (var item in accessFolders)
            {
                var folderModel = new FolderModel
                {
                    Path = item.Item2.Path,
                    Name = item.Item2.Name
                };
                var token = item.Item1;
                folderModel.RemoveFolderButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(
                    async (e) =>
                    {
                        await FutureAccessListHelper.Instance.RemoveAuthorizedItemAsync(token);
                        AccessFolders.Remove(folderModel);
                    });
                AccessFolders.Add(folderModel);
            }
        }

        /// <summary>
        /// Handle folder removal.
        /// </summary>
        /// <param name="e">Param for removal operation.</param>
        private async void OnRemoveFolderButtonClicked(RoutedEventArgs e)
        {
            try
            {
                var ctx = (FolderModel)((Button)e.OriginalSource).DataContext;
                var libraryFolders = await StorageLibrary.GetLibraryAsync(KnownLibraryId.Music);
                var folder = libraryFolders.Folders.SingleOrDefault((f) => f.Path == ctx.Path);
                if (folder == null) return;
                if (await libraryFolders.RequestRemoveFolderAsync(folder))
                    LibraryFolders.Remove(ctx);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Post notify external workers for heavy workloads.
        /// </summary>
        public async void PostNotifyExternalWorkers()
        {
            if (IsTrackLibraryChangesEnabled)
            {
                await LibraryService.StartChangeTrackingAsync();
            }
            else
            {
                LibraryService.StopChangeTracking();
            }
        }

        /// <summary>
        /// Method for cleaning up.
        /// </summary>
        public override void Cleanup()
        {
            ExcludedFolders.Clear();
            LibraryFolders.Clear();
            AccessFolders.Clear();
        }
    }
}
