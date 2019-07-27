using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using GalaSoft.MvvmLight;
using Light.Common;
using Light.Managed.Feedback;
using Light.Managed.Feedback.Models;
using Light.Managed.Tools;

namespace Light.ViewModel.Feedback
{
    /// <summary>
    /// Feedback view model for feedback view.
    /// </summary>
    public class FeedbackViewModel : ViewModelBase
    {
        private bool _isImageUploadingFinished;
        private double _imageUploadProgressValue;
        private readonly CoreDispatcher _dispatcher;
        private readonly string _imagePath;
        private string _title;
        private string _content;
        private string _contact;
        private bool _isBugChecked;
        private bool _isSuggestionChecked;
        private bool _isUploadingImageChecked;
        private SubmitFeedbackCommand _command;
        private bool _prevState;
        private bool _isContentEditable;

        /// <summary>
        /// A value indicates whether image upload is started.
        /// </summary>
        public bool IsImageUploadingFinished
        {
            get { return _isImageUploadingFinished; }
            set
            {
                _isImageUploadingFinished = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates current image upload progress.
        /// </summary>
        public double ImageUploadProgressValue
        {
            get { return _imageUploadProgressValue; }
            set
            {
                _imageUploadProgressValue = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// An value indicates whether uploading is in progress.
        /// </summary>
        public bool IsContentEditable
        {
            get { return _isContentEditable; }
            set
            {
                _isContentEditable = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates whether there is a valid screenshot.
        /// </summary>
        public bool CanAttachScreenshot => _imagePath != null;

        /// <summary>
        /// Feedback title.
        /// </summary>
        public string Title
        {
            get { return _title; }
            set
            {
                if (_title != value)
                {
                    _title = value;
                    RaisePropertyChanged();
                    EvalulateContent();
                } 
            }
        }

        /// <summary>
        /// Feedback content.
        /// </summary>
        public string Content
        {
            get { return _content; }
            set
            {
                _content = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Feedback contact.
        /// </summary>
        public string Contact
        {
            get { return _contact; }
            set
            {
                _contact = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// A value indicates whether bug radio button is checked.
        /// </summary>
        public bool IsBugChecked
        {
            get { return _isBugChecked; }
            set
            {
                _isBugChecked = value;
                RaisePropertyChanged();
                EvalulateContent();
            }
        }

        /// <summary>
        /// A value indicates whether suggestion radio button is checked.
        /// </summary>
        public bool IsSuggestionChecked
        {
            get { return _isSuggestionChecked; }
            set
            {
                _isSuggestionChecked = value;
                RaisePropertyChanged();
                EvalulateContent();
            }
        }

        /// <summary>
        /// A value indicates whether upload image or not.
        /// </summary>
        public bool IsUploadingImageChecked
        {
            get { return _isUploadingImageChecked; }
            set
            {
                _isUploadingImageChecked = value;
                RaisePropertyChanged();
                EvalulateContent();
            }
        }

        /// <summary>
        /// Event command for feedback submitting.
        /// </summary>
        public SubmitFeedbackCommand Command
        {
            get { return _command; }
            set
            {
                _command = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public FeedbackViewModel(string imagePath)
        {
            _dispatcher = Window.Current.Dispatcher;
            _imagePath = imagePath;
            _prevState = false;

            IsImageUploadingFinished = false;

            // Check Internet connection
            IsContentEditable = false;
            ImageUploadProgressValue = 0.0;

            Command = new SubmitFeedbackCommand(this);
        }

        /// <summary>
        /// Validate internet connection.
        /// </summary>
        /// <returns>An awaitable task.</returns>
        public async Task ValidateInternetConnectionAsync()
        {
            if (await Command.ValidateInternetConnectionAsync())
            {
                IsContentEditable = true;
            }
        }

        /// <summary>
        /// A value indicates whether content can be submitted.
        /// </summary>
        private bool CanSubmitted
        {
            get
            {
                // Either option must be selected
                var typeValidation = !IsSuggestionChecked && !IsBugChecked;
                if (typeValidation)
                {
                    return false;
                }

                // Title must be filled
                if (string.IsNullOrEmpty(Title))
                {
                    return false;
                }

                // Otherwise it can be accepted
                return true;
            }
        }

        /// <summary>
        /// Fired when the content's legality changed.
        /// </summary>
        public event EventHandler<bool> CanSubmittedChanged;

        /// <summary>
        /// Evalulate content when specific entry changes.
        /// </summary>
        private void EvalulateContent()
        {
            if (_prevState != CanSubmitted)
            {
                _prevState = CanSubmitted;
                CanSubmittedChanged?.Invoke(this, _prevState);
            }
        }

        /// <summary>
        /// Submit feedback command.
        /// </summary>
        public class SubmitFeedbackCommand : ICommand
        {
            private readonly FeedbackViewModel _parentViewModel;

            /// <summary>
            /// Class constructor.
            /// </summary>
            /// <param name="viewModel"></param>
            public SubmitFeedbackCommand(FeedbackViewModel viewModel)
            {
                _parentViewModel = viewModel;
                _parentViewModel.CanSubmittedChanged += ParentViewModelOnCanSubmittedChanged;
            }

            /// <summary>
            /// Event handler for content legality changes.
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="b"></param>
            private void ParentViewModelOnCanSubmittedChanged(object sender, bool b)
            {
                CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            }

            /// <summary>
            /// Query whether the action can be executed.
            /// </summary>
            /// <param name="parameter"></param>
            /// <returns></returns>
            public bool CanExecute(object parameter)
            {
                return _parentViewModel.CanSubmitted;
            }

            /// <summary>
            /// Execute the action.
            /// </summary>
            /// <param name="parameter"></param>
            public async void Execute(object parameter)
            {
                // Lock content.
                _parentViewModel.IsContentEditable = false;

                // Validate network.
                if (!InternetConnectivityDetector.HasInternetConnection)
                {
                    await ShowInetConnectionFailedMessageDialogAsync();
                    _parentViewModel.IsContentEditable = true;
                    return;
                }

                ImageUploadResult imageResult = null;

                // If image is present, starting uploading.
                if (_parentViewModel.IsUploadingImageChecked)
                {
                    // Reset values.
                    _parentViewModel.IsImageUploadingFinished = false;
                    _parentViewModel.ImageUploadProgressValue = 0.0;

                    try
                    {
                        var file = await StorageFile.GetFileFromPathAsync(_parentViewModel._imagePath);
                        var task = file.UploadImageAsync();
                        // Report progress to UI.
                        task.Progress += async (info, progressInfo) =>
                        {
                            await _parentViewModel._dispatcher.RunAsync(CoreDispatcherPriority.High,
                                () => _parentViewModel.ImageUploadProgressValue = progressInfo);
                        };
                        // Await result.
                        imageResult = await task;
                    }
                    catch (RequestThrottledException)
                    {
                        await HandleThrottledRequestsAsync();
                        return;
                    }
                    catch (HttpRequestException)
                    {
                        await HandleErrorsAsync();
                        return;
                    }
                    catch (FileNotFoundException)
                    {
                        // Ignore file
                    }
                    catch (FeedbackServerErrorException)
                    {
                        await HandleErrorsAsync();
                        return;
                    }
                    catch (COMException)
                    {
                        await ShowCertIssueAsync();
                        return;
                    }
                }

                // Upload feedback.
                _parentViewModel.IsImageUploadingFinished = true;
                try
                {
                    var type = (_parentViewModel.IsBugChecked) ? FeedbackType.Bug : FeedbackType.Suggestion;

                    // To prevent Akismet false rejections, place title into content if content is empty.
                    if (string.IsNullOrEmpty(_parentViewModel.Content))
                    {
                        _parentViewModel.Content = _parentViewModel.Title;
                    }

                    await FeedbackClient.SendfeedbackAsync(type, _parentViewModel.Title, _parentViewModel.Content,
                        _parentViewModel.Contact, imageResult);
                }
                catch (RequestThrottledException)
                {
                    await HandleThrottledRequestsAsync();
                    return;
                }
                catch (HttpRequestException)
                {
                    await HandleErrorsAsync();
                    return;
                }
                catch (FeedbackServerErrorException)
                {
                    await HandleErrorsAsync();
                    return;
                }
                catch (COMException)
                {
                    await ShowCertIssueAsync();
                    return;
                }

                // Clean up
                await ShowFinishedAsync();
            }

            public event EventHandler CanExecuteChanged;

            /// <summary>
            /// General method for showing dialogs.
            /// </summary>
            /// <param name="dialogTag"></param>
            /// <returns></returns>
            private async Task ShowMessageDialogAsync(string dialogTag)
            {
                var messageDialog = new MessageDialog(CommonSharedStrings.GetString($"{dialogTag}Message"),
                    CommonSharedStrings.GetString($"{dialogTag}Title"));
                await messageDialog.ShowAsync();
            }

            /// <summary>
            /// Message dialog for errors.
            /// </summary>
            private async Task HandleErrorsAsync()
            {
                _parentViewModel.IsContentEditable = true;
                await ShowMessageDialogAsync("InternalError");
            }

            /// <summary>
            /// Message dialog for throttled requests.
            /// </summary>
            /// <returns></returns>
            private async Task HandleThrottledRequestsAsync()
            {
                _parentViewModel.IsContentEditable = true;
                await ShowMessageDialogAsync("RequestThrottled");
            }

            /// <summary>
            /// Message dialog for finished requests.
            /// </summary>
            /// <returns></returns>
            private async Task ShowFinishedAsync()
            {
                await ShowMessageDialogAsync("FeedbackAccepted");

                // Close this window
                Window.Current.Close();
            }

            /// <summary>
            /// Message dialog for certification verification failure.
            /// </summary>
            /// <returns></returns>
            private async Task ShowCertIssueAsync()
            {
                await ShowMessageDialogAsync("CertValidationFailure");

                // Close this window
                Window.Current.Close();
            }

            /// <summary>
            /// Method to validate Internet connection.
            /// </summary>
            public async Task<bool> ValidateInternetConnectionAsync()
            {
                try
                {
                    if (!InternetConnectivityDetector.HasInternetConnection)
                    {
                        await ShowInetConnectionFailedMessageDialogAsync();
                        return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    TelemetryHelper.TrackExceptionAsync(ex);
                    await ShowInetConnectionFailedMessageDialogAsync();
                }

                return false;
            }

            /// <summary>
            /// Method to show "no connection" dialog.
            /// </summary>
            /// <returns></returns>
            private async Task ShowInetConnectionFailedMessageDialogAsync()
            {
                await ShowMessageDialogAsync("NetworkConnection");
                Window.Current.Close();
            }
        }
    }
}
