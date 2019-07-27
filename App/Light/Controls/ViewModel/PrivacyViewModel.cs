using GalaSoft.MvvmLight;
using Light.Managed.Settings;

namespace Light.Controls.ViewModel
{
    /// <summary>
    /// Privacy sub-viewmodel for common settings viewmodel.
    /// </summary>
    public class PrivacyViewModel : ViewModelBase
    {
        const string TelemetryKey = "OptinTelemetry";

        private bool _isOptinTelemetry;

        /// <summary>
        /// Indicates whether opt-in telemetry or not.
        /// </summary>
        public bool IsOptinTelemetry
        {
            get { return _isOptinTelemetry; }
            set
            {
                _isOptinTelemetry = value;
                RaisePropertyChanged();
                SettingsManager.Instance.SetValue(value, TelemetryKey);
            }
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        public PrivacyViewModel()
        {
            _isOptinTelemetry = SettingsManager.Instance.GetValue<bool>(TelemetryKey);
        }
    }
}
