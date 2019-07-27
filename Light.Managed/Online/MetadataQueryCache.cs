using System.Collections.Generic;
using Light.Managed.Database;

namespace Light.Managed.Online
{
    public class MetadataQueryCache<T, TK>
    {
        private readonly string _tFullName;
        private readonly string _tkFullName;
        private readonly Dictionary<string, string> _localQueryCache;
        private readonly string _cacheType;

        public MetadataQueryCache(string type)
        {
            _tFullName = typeof (T).FullName;
            _tkFullName = typeof (TK).FullName;
            _localQueryCache = new Dictionary<string, string>();
            _cacheType = type;
        }

        public void Add(string key, string content)
        {
            if (!_localQueryCache.ContainsKey(key))
                _localQueryCache.Add(key, content);
        }

        public void Remove(string key)
        {
            if (_localQueryCache.ContainsKey(key))
                _localQueryCache.Remove(key);
        }

        public bool ContainsKey(string key)
        {
            return _localQueryCache.ContainsKey(key);
        }

        public string Get(string key)
        {
            if (_localQueryCache.ContainsKey(key))
            {
                return _localQueryCache[key];
            }

            return null;
        }
    }
}
