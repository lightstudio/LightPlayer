using System.Windows.Input;
using Newtonsoft.Json;

namespace Light.Model
{
    /// <summary>
    /// Model class for media file properties view.
    /// </summary>
    public class MediaPropertyItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public ICommand Copy { get; set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        [JsonConstructor]
        public MediaPropertyItem()
        {
            
        }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="copyCommand"></param>
        public MediaPropertyItem(string key, string value, ICommand copyCommand)
        {
            Key = key;
            Value = value;
            Copy = copyCommand;
        }
    }
}
