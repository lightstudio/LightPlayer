using System.Collections;
using System.Collections.Generic;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace Light.Managed.Online
{
    /// <summary>
    /// Metadata banlist with iterator.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    public class MetadataRetrieveBanlist<T> : IEnumerable<string>
    {
        private readonly IPropertySet _values;

        /// <summary>
        /// Class constructor.
        /// </summary>
        public MetadataRetrieveBanlist()
        {
            var container = ApplicationData.Current.LocalSettings.CreateContainer(typeof(T).FullName,
                ApplicationDataCreateDisposition.Always);
            _values = container.Values;
        }

        /// <summary>
        /// Add an element to banlist.
        /// </summary>
        /// <param name="content">To content to be banned.</param>
        public void Add(string content)
        {
            if (!_values.ContainsKey(content))
                _values.Add(content, content);
        }

        /// <summary>
        /// Remove an element from banlist.
        /// </summary>
        /// <param name="content"></param>
        public void Remove(string content)
        {
            if (_values.ContainsKey(content))
                _values.Remove(content);
        }

        /// <summary>
        /// Check if a specificed element is in banlist.
        /// </summary>
        /// <param name="content">The content to be checked.</param>
        /// <returns>A boolean indicates whether the content is in banlist.</returns>
        public bool ContainsKey(string content)
        {
            return _values.ContainsKey(content);
        }

        /// <summary>
        /// Get banlist content enumerator.
        /// </summary>
        /// <returns>Content enumerator.</returns>
        public IEnumerator<string> GetEnumerator()
        {
            return _values.Keys.GetEnumerator();
        }

        /// <summary>
        /// Get banlist content enumerator.
        /// </summary>
        /// <returns>Content enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
