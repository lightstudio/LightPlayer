using System.Xml.Serialization;

namespace Light.Managed.Settings.Model
{
    /// <remarks/>
    [XmlType(AnonymousType = true, Namespace = "http://schemas.ligstd.com/schema/SettingsProvisionFile.xsd")]
    [XmlRoot(Namespace = "http://schemas.ligstd.com/schema/SettingsProvisionFile.xsd", IsNullable = false)]
    public class SettingsProvisionFile
    {
        /// <remarks/>
        [XmlElement("SettingsEntry")]
        public SettingsProvisionFileSettingsEntry[] SettingsEntry { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string Id { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string Version { get; set; }
    }

    /// <remarks/>
    [XmlType(AnonymousType = true, Namespace = "http://schemas.ligstd.com/schema/SettingsProvisionFile.xsd")]
    public class SettingsProvisionFileSettingsEntry
    {
        /// <remarks/>
        public SettingsProvisionFileSettingsEntryAvailability Availability { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string Key { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string Value { get; set; }

        /// <remarks/>
        [XmlAttribute]
        public string Type { get; set; }
    }

    /// <remarks/>
    [XmlType(AnonymousType = true, Namespace = "http://schemas.ligstd.com/schema/SettingsProvisionFile.xsd")]
    public class SettingsProvisionFileSettingsEntryAvailability
    {
        /// <remarks/>
        public string DeviceFamily { get; set; }
    }
}
