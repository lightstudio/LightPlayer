using System;
using Light.Common;
using Light.Model;
using Windows.ApplicationModel;
using Windows.System;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml;
using System.IO;
using Windows.UI.Xaml.Documents;

namespace Light.Flyout
{
    /// <summary>
    /// Flyout that represents application and environment information.
    /// </summary>
    public sealed partial class AboutFlyout : ContentDialog
    {
        private BuildInfo BuildInfo = new BuildInfo();
        private const string BuildVerTemplate = "{0}.{1}.{2}.{3}";
        private bool m_bLicenseLoaded = false;

        /// <summary>
        /// ETW logger ID for customers.
        /// </summary>
        public string EtwLoggerId => ApplicationServiceBase.App.EtwChannelId.ToString();

        /// <summary>
        /// Class constructor that creates instance of <see cref="AboutFlyout"/>.
        /// </summary>
        public AboutFlyout()
        {
            InitializeComponent();
            Loaded += FlyoutLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            EtwIdTextBlock.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Event handler for flyout loaded.
        /// </summary>
        /// <param name="sender">Instance of <see cref="AboutFlyout"/>.</param>
        /// <param name="e">Instance of <see cref="RoutedEventArgs"/>.</param>
        private void FlyoutLoaded(object sender, RoutedEventArgs e)
        {
            LoadInformation();
        }

        /// <summary>
        /// Load application and buld information.
        /// </summary>
        private void LoadInformation()
        {
            var resourceLoader = ResourceLoader.GetForViewIndependentUse();
            var packageIdentity = Package.Current.Id;

            BuildInfo.VersionString =
                string.Format(
                    BuildVerTemplate,
                    packageIdentity.Version.Major,
                    packageIdentity.Version.Minor,
                    packageIdentity.Version.Build,
                    packageIdentity.Version.Revision);

            if (PlatformInfo.IsRedstoneRelease)
            {
                BuildInfo.Branch = (Package.Current.SignatureKind == PackageSignatureKind.Store) ?
                    resourceLoader.GetString(CommonSharedStrings.StoreReleaseKey) :
                    resourceLoader.GetString(CommonSharedStrings.SideLoadKey);
            }

            var processorIdentifier = CommonSharedStrings.ArchUnknown;
            switch (packageIdentity.Architecture)
            {
                case ProcessorArchitecture.Arm:
                    processorIdentifier = CommonSharedStrings.ArchARM;
                    break;
                case ProcessorArchitecture.Neutral:
                    processorIdentifier = CommonSharedStrings.ArchNetural;
                    break;
                case ProcessorArchitecture.X64:
                    processorIdentifier = CommonSharedStrings.Archx64;
                    break;
                case ProcessorArchitecture.X86:
                    processorIdentifier = CommonSharedStrings.Archx86;
                    break;
            }

            BuildInfo.BuildEnv = resourceLoader.GetString(processorIdentifier);
        }

        /// <summary>
        /// Event handler of clicking OSS license link.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void OnOssLicenseLinkClick(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (!m_bLicenseLoaded)
            {
                var file = await Package.Current.InstalledLocation.GetFileAsync(CommonSharedStrings.OssLicenseFileName);
                using (var rsInput = await file.OpenReadAsync())
                using (var sInput = rsInput.AsStream())
                using (var sReader = new StreamReader(sInput))
                {
                    string szInput = null;
                    while ((szInput = await sReader.ReadLineAsync()) != null)
                    {
                        var para = new Paragraph();
                        var run = new Run { Text = szInput };
                        para.Inlines.Add(run);
                        m_rtbLicenseTextBlock.Blocks.Add(para);
                    }
                }

                m_hlbtnLicenseLink.Visibility = Visibility.Collapsed;
                m_svLicenseContainer.Visibility = Visibility.Visible;
            }
        }

        private void OnLightTileDoubleTapped(object sender, Windows.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
        {
            EtwIdTextBlock.Visibility = Visibility.Visible;
        }
    }
}
