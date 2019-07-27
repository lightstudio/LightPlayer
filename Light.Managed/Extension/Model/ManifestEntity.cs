#if ENABLE_STAGING
using System.Collections.Generic;

namespace Light.Managed.Extension.Model
{
    class ManifestEntity
    {
        public string Name { get; set; }
        public string Author { get; set; }
        public int Version { get; set; }
        public Arch Arch { get; set; }
        public PackageType Type { get; set; }
        public string EntryPoint { get; set; }
        public List<string> SupportedFormats { get; set; }
        public List<SecurityEntity> SecurityInfo { get; set; }  
        public string Intro { get; set; }
        public string Website { get; set; }
    }

    class SecurityEntity
    {
        public string File { get; set; }
        public string Hash { get; set; }
    }
}
#endif