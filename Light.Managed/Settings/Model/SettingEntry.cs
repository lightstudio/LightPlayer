using System;

namespace Light.Managed.Settings.Model
{
    internal class SettingEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public object Value { get; set; }
        public Type ValueType { get; set; }
    }
}
