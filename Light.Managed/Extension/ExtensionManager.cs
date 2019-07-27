#if ENABLE_STAGING
using Light.Managed.Database.Native;
using Light.Managed.Extension.Model;
using Light.Managed.Settings;
using Light.Managed.Tools;
using Light.NETCore.IO;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Storage;

namespace Light.Managed.Extension
{

    public class ExtensionManager
    {
        private ExtensionDatabaseWorker _databaseWorker;

        public ExtensionManager(ExtensionDatabaseWorker databaseWorker)
        {
            _databaseWorker = databaseWorker;
        }

#region Internal Helper Methods
        internal static void GenerateEncryptionKey()
        {
            var randomData = CryptographicBuffer.GenerateRandom(64);
            var randomDataSerialized = CryptographicBuffer.EncodeToHexString(randomData);

            SettingsManager.Instance.SetValue(randomDataSerialized, "EncryptionKey");
        }

        internal static string GenerateFolderName()
        {
            var randomData = CryptographicBuffer.GenerateRandom(48);
            return CryptographicBuffer.EncodeToHexString(randomData);
        }

        internal static async Task<IStorageFolder> CreateFolder()
        {
            IStorageFolder extRootFolder;
            if (
                NativeMethods.GetStorageFolderFromPath(
                    Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Extensions"), out extRootFolder) != 0)
            {
                return await
                    ApplicationData.Current.LocalCacheFolder.CreateFolderAsync("Extensions",
                        CreationCollisionOption.OpenIfExists);
            }
            return extRootFolder;
        }

        internal string SerializeFormats(IEnumerable<string> formats)
        {
            var rel = formats.Aggregate("", (current, f) => current + $"f;");
            return rel.Substring(0, rel.Length - 2);
        }

        internal string EncryptData(string data)
        {
            if (!SettingsManager.Instance.ContainsKey("EncryptionKey")) GenerateEncryptionKey();

            var key =
                CryptographicBuffer.DecodeFromHexString(
                    SettingsManager.Instance.GetValue<string>("EncryptionKey"));

            return string.Empty;
        }
#endregion

#region Installer
        public async Task<InstallResult> InstallPackageAsync(StorageFile package)
        {
            var folder = await CreateFolder();
            var workFolder = await folder.CreateFolderAsync(GenerateFolderName());
            if (await Decompressor.UnZipFile(package, workFolder))
            {
                // Find manifest.
                IStorageFile manifestFile;
                IStorageFile sigFile;
                if (NativeMethods.GetStorageFileFromPath(Path.Combine(workFolder.Path, "_signature"), out sigFile) != 0)
                {
                    return InstallResult.FileError;
                }
                if (
                    NativeMethods.GetStorageFileFromPath(Path.Combine(workFolder.Path, "Manifest.json"),
                        out manifestFile) != 0)
                {
                    return InstallResult.FileError;
                }
                else
                {
                    // Read manifest content.
                    var sigContent = await FileIO.ReadTextAsync(sigFile);
                    var manifestContent = await FileIO.ReadTextAsync(manifestFile);

                    // Verify security.
                    if (!await SecurityHelper.VerifyManifestSecurity(manifestContent, sigContent))
                        return InstallResult.IntegrityFailure;

                    // Deserialize content.
                    if (!string.IsNullOrEmpty(manifestContent))
                    {
                        var m = JsonConvert.DeserializeObject<ManifestEntity>(manifestContent);
                        // Verify security
                        var verifyContentKvPairs =
                            m.SecurityInfo.Select(
                                i => new KeyValuePair<string, string>(Path.Combine(workFolder.Path, i.File), i.Hash));
                        if (!await SecurityHelper.VerifyHash(verifyContentKvPairs))
                            return InstallResult.IntegrityFailure;

                        // Verify Architecture
#if WIN32
                        if (m.Arch != Arch.Win32 && m.Arch != Arch.Universal) return InstallResult.ArchitectureFailure;
#endif
#if ARM
                        if (m.Arch != Arch.WoA && m.Arch != Arch.Universal) return InstallResult.ArchitectureFailure;
#endif
                        // Register Package.
                        var extPkg = new ExtPackage
                        {
                            Arch = m.Arch,
                            EntryPoint = m.EntryPoint,
                            Name = m.Name,
                            PackageId = workFolder.Name,
                            SupportedFormats = SerializeFormats(m.SupportedFormats),
                            Version = m.Version,
                            Type = m.Type
                        };

                        _databaseWorker.RegisterPackage(extPkg);
                    }
                }

                return InstallResult.Succeeded;
            }
            else return InstallResult.FileError; 
        }

        public async Task<InstallResult> UninstallPackageAsync(string packageId)
        {
            // Post unregister in database
            try
            {
                var rootFolder = await CreateFolder();
                var workFolder = await rootFolder.GetFolderAsync(packageId);
                foreach (var f in await workFolder.GetFilesAsync()) await f.DeleteAsync();
                _databaseWorker.UnregisterPackage(packageId);
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }

            return InstallResult.Succeeded;
        }
#endregion

#region Internal

        internal void RegisterInternalDecoder()
        {
            _databaseWorker.RegisterPackageAndFormat(new ExtPackage
            {
#if WIN32
                    Arch = Arch.Win32,
#endif
#if WOA
                    Arch = Arch.WoA,
#endif
                Name = "Internal FFMPEG Decoder (LGPL)",
                EntryPoint = "Light.dll",
                PackageId = "LightStudio.Internal.Decoder.Ffmpeg.LGPL",
                SecurityData = "",
                SupportedFormats = ".mp3;.mp2;.wma;.aac;.m4a;.wav;.flac;.ape;.tta;.tak;.mka;.ogg",
                Type = PackageType.Decoder,
                Version = 1
            });
        }

#endregion

#region External Decoder field

        public async Task<string> QueryPreferredDecoderId(string ext)
        {
            return await Task.Run(() =>
            {
                return _databaseWorker.FindPreferredDecoder(ext);
            });
        }

        public async Task<string> QueryPreferredDecoderPath(string id)
        {
            return await Task.Run(() =>
            {
                return _databaseWorker.FindDecoderFileById(id);
            });
        } 

#endregion
    }

}
#endif
