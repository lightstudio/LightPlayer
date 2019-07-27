using System;

namespace Light.Model
{
    /// <summary>
    /// Data entity represents a settings section.
    /// </summary>
    public class SettingsSection
    {
        /// <summary>
        /// Name of the settings section.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Glyph to represent the settings section category.
        /// </summary>
        public string Glyph { get; }

        /// <summary>
        /// Type of the settings section.
        /// </summary>
        public SettingsType Type { get; }

        /// <summary>
        /// Type of the settings section category.
        /// </summary>
        public enum SettingsType
        {
            Interface = 0,
            Library = 1,
            Playback = 2,
            Privacy = 3,
            Debug = 4
        }

        /// <summary>
        /// Initializes new instance of <see cref="SettingsSection"/>.
        /// </summary>
        /// <param name="name">Name of the settings section.</param>
        /// <param name="glyph">Glpyh to represent the settings section category.</param>
        /// <param name="type">Type of the settings section.</param>
        public SettingsSection(string name, string glyph, SettingsType type)
        {
            Name = name;
            Glyph = glyph;
            Type = type;
        }
    }
}
