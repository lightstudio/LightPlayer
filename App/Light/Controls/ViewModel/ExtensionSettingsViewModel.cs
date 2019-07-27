using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Light.Common;
using Light.Controls.Models;
using Light.Lyrics.External;
using RelayCommand = GalaSoft.MvvmLight.Command.RelayCommand;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// Extension sub-viewmodel for common settings viewmodel.
    /// </summary>
    public class ExtensionSettingsViewModel : ViewModelBase
    {
        private ObservableCollection<LrcSourceModel> _lrcSources;
        private readonly RelayCommand<RoutedEventArgs> _removeLrcSourceButtonClickedRelayCommand;

        /// <summary>
        /// A collection of lyric providers.
        /// </summary>
        public ObservableCollection<LrcSourceModel> LrcSources
        {
            get { return _lrcSources; }
            set { Set(ref _lrcSources, value); }
        }

        /// <summary>
        /// Handler for adding lyric button.
        /// </summary>
        public RelayCommand AddLrcSourceRelayCommand { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public ExtensionSettingsViewModel()
        {
            LrcSources = new ObservableCollection<LrcSourceModel>();

            AddLrcSourceRelayCommand = new RelayCommand(AddLrcSourceStub);
            _removeLrcSourceButtonClickedRelayCommand = new RelayCommand<RoutedEventArgs>(OnRemoveLrcSourceButtonClicked);
        }

        /// <summary>
        /// Handle adding lyric source.
        /// </summary>
        private async void AddLrcSourceStub()
        {
            try
            {
                FileOpenPicker picker = new FileOpenPicker
                {
                    ViewMode = PickerViewMode.List,
                    SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                };
                picker.FileTypeFilter.Add(CommonSharedStrings.JavaScriptFileFormatSuffix);

                var files = await picker.PickMultipleFilesAsync();
                foreach (var file in files)
                {
                    var name = file.DisplayName;
                    if (LrcSources.Any(source => source.Name == name))
                    {
                        // TODO: let user choose to rename or overwrite.
                    }

                    var text = await FileIO.ReadTextAsync(file);
                    SourceScriptManager.AddScript(name, text);
                    LrcSources.Add(new LrcSourceModel
                    {
                        Name = name,
                        RemoveLrcSourceButtonClickedRelayCommand = _removeLrcSourceButtonClickedRelayCommand
                    });
                }
            }
            catch (SecurityException)
            {
                // Ignore, notify user
            }
            catch (COMException)
            {
                // Ignore, notify user
            }
            catch (FileNotFoundException)
            {
                // Ignore, notify user
            }
        }

        /// <summary>
        /// Method for loading lyric sources.
        /// </summary>
        public void LoadLyricsSources()
        {
            var scripts = SourceScriptManager.GetAllScripts();
            foreach (var s in scripts)
            {
                LrcSources.Add(new LrcSourceModel
                {
                    Name = s.Name,
                    RemoveLrcSourceButtonClickedRelayCommand = _removeLrcSourceButtonClickedRelayCommand
                });
            }
        }

        /// <summary>
        /// Handle lyric source removal.
        /// </summary>
        /// <param name="e">Param for removal operation.</param>
        private void OnRemoveLrcSourceButtonClicked(RoutedEventArgs e)
        {
            var ctx = (LrcSourceModel)((Button)e.OriginalSource).DataContext;
            SourceScriptManager.RemoveScript(ctx.Name);
            LrcSources.Remove(ctx);
        }

        /// <summary>
        /// Method for clean up.
        /// </summary>
        public override void Cleanup()
        {
            LrcSources.Clear();
            base.Cleanup();
        }
    }
}
