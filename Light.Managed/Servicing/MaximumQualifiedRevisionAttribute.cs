using System;

namespace Light.Managed.Servicing
{
    /// <summary>
    /// Specifies servicing task's applicability by specifying maximum qualified revision.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class MaximumQualifiedRevisionAttribute : Attribute
    {
        /// <summary>
        /// Maximum qualified reversion.
        /// </summary>
        public ushort Revision { get; }

        /// <summary>
        /// Initializes new instance of <see cref="MaximumQualifiedRevisionAttribute"/>.
        /// </summary>
        /// <param name="revision">Maximum qualified revision.</param>
        public MaximumQualifiedRevisionAttribute(ushort revision)
        {
            Revision = revision;
        }
    }
}
