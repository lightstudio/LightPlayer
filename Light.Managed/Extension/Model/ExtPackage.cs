#if ENABLE_STAGING
using System.ComponentModel.DataAnnotations;

namespace Light.Managed.Extension.Model
{
    public class ExtPackage
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; }
        public Arch Arch { get; set; }
        public PackageType Type { get; set; }
        [Required]
        public string EntryPoint { get; set; }
        public int Version { get; set; }
        public string SecurityData { get; set; }
        public string SupportedFormats { get; set; }
        public string PackageId { get; set; }
    }

    public enum PackageType
    {
        Decoder = 0,
        Assets = 1,
        OnlineProvider = 2
    }

    public enum Arch
    {
        Win32 = 0,
        WoA = 1,
        Universal = 2
    }

    public enum InstallResult
    {
        Succeeded = 0,
        FileError = 1,
        IntegrityFailure = 2,
        ArchitectureFailure = 3,
        OutOfDate = 4,
        VersionFailure = 5,
        UnspecificedError = 6
    }

    public class FormatTable
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string PluginId { get; set; }
        [Required]
        public string Format { get; set; }
    }
}
#endif