using Light.Common;
using Light.Managed.Tools;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Light
{
    sealed partial class App
    {
        /// <summary>
        /// Convert exception to message to avoid free-after-access issue in CoreCLR/Windows Runtime.
        /// </summary>
        /// <param name="ex">Instance of <see cref="Exception"/>.</param>
        /// <returns>Serialized exception message.</returns>
        private string ExceptionToMessage(Exception ex)
        {
            var sbExceptionMsg = new StringBuilder();
            Exception current = ex;
            while (current != null)
            {
                sbExceptionMsg.AppendLine(current.GetType().Name);
                sbExceptionMsg.AppendLine(current.Message);
                sbExceptionMsg.AppendLine(current.StackTraceEx());
                sbExceptionMsg.AppendLine("");
                current = current.InnerException;
            }
            return sbExceptionMsg.ToString();
        }

        /// <summary>
        /// Handles synchronous unhandled exception.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Instance of <see cref="UnhandledExceptionEventArgs"/>.</param>
        private async void OnUnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.Exception;
                e.Handled = true;

                var strExceptionMsg = ExceptionToMessage(ex);
                await LogException(strExceptionMsg);
                await NotifyException(strExceptionMsg);

                TelemetryHelper.LogSerializedException(strExceptionMsg);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Handles asynchronous unhandled exception.
        /// </summary>
        /// <param name="sender">Event source.</param>
        /// <param name="e">Instance of <see cref="UnhandledExceptionEventArgs"/>.</param>
        private async void OnAsyncUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                var ex = e.Exception;
                e.Handled = true;

                var strExceptionMsg = ExceptionToMessage(ex);
                await LogException(strExceptionMsg);
                await NotifyException(strExceptionMsg);

                TelemetryHelper.LogSerializedException(strExceptionMsg);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }

        /// <summary>
        /// Register exception handling synchronization context to app domain.
        /// </summary>
        private void RegisterExceptionHandlingSynchronizationContext()
        {
            ExceptionHandlingSynchronizationContext
                .Register()
                .UnhandledException += OnAsyncUnhandledException;
        }

        /// <summary>
        /// Log serialized exception to disk asynchronously.
        /// </summary>
        /// <param name="strSerializedException">Serialized exception.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        private async Task LogException(string strSerializedException)
        {
            try
            {
                var logPath = Path.Combine(
                    ApplicationData.Current.LocalFolder.Path, 
                    string.Format(CommonSharedStrings.CrashLogFilenameTemplate, 
                    DateTime.Now.Ticks));
                var logContent = string.Format(CommonSharedStrings.CrashLogFileTemplate, strSerializedException);
                await Task.Factory.StartNew(() => File.WriteAllText(logPath, logContent));
            }
            catch (Exception e)
            {
                TelemetryHelper.TrackExceptionAsync(e);
            }
        }

        /// <summary>
        /// Present a content dialog that contains unhandled exception to user asynchronously.
        /// </summary>
        /// <param name="strSerializedException">Serialized exception.</param>
        /// <returns>Task represents the asynchronous operation.</returns>
        private async Task NotifyException(string strSerializedException)
        {
            try
            {
                if (_ignoreException) return;

                var dialog = new ContentDialog
                {
                    FullSizeDesired = false,
                    PrimaryButtonText = CommonSharedStrings.UnknownErrorPromptMainButtonText,
                    IsPrimaryButtonEnabled = true,
                    Title = CommonSharedStrings.UnknownErrorMessageTitle,
                    MaxWidth = ApplicationView.GetForCurrentView().VisibleBounds.Width,
                    Style = (Style)Current.Resources["LightContentDialogStyle"]
                };

                var exceptionPanel = new StackPanel();
                exceptionPanel.Children.Add(
                new TextBlock
                {
                    Text = string.Format(CommonSharedStrings.UnknownErrorPromptContent,
                        strSerializedException),
                    TextWrapping = TextWrapping.Wrap
                });

                var ignoreBox = new CheckBox { Content = CommonSharedStrings.SuppressUnknownErrorPromptText };
                ignoreBox.Checked += (sender, args) => _ignoreException = true;
                ignoreBox.Unchecked += (sender, args) => _ignoreException = false;

                var scrollViewer = new ScrollViewer { Content = exceptionPanel };

                exceptionPanel.Children.Add(ignoreBox);
                dialog.Content = scrollViewer;
                await dialog.ShowAsync();
            }
            catch
            {
                //ignore exceptions here to prevent unhandled exception loop
            }
        }
    }
}
