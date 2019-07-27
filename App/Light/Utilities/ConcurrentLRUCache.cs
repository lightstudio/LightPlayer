using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities
{
    public class ConcurrentLRUCache<K, V>
    {
        public class CacheItem<Key, Value>
        {
            public CacheItem(Key k, Value v)
            {
                key = k;
                value = v;
            }
            public Key key;
            public Value value;
        }

        private object l = new object();
        private int capacity;
        private Dictionary<K, LinkedListNode<CacheItem<K, V>>> cacheMap = new Dictionary<K, LinkedListNode<CacheItem<K, V>>>();
        private LinkedList<CacheItem<K, V>> lruList = new LinkedList<CacheItem<K, V>>();

        public ConcurrentLRUCache(int capacity)
        {
            this.capacity = capacity;
        }
        public V GetValue(K key)
        {
            lock (l)
            {
                LinkedListNode<CacheItem<K, V>> node;
                if (cacheMap.TryGetValue(key, out node))
                {
                    V value = node.Value.value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                    return value;
                }
                return default(V);
            }
        }

        public bool TryGetValue(K key, out V value)
        {
            lock (l)
            {
                if (cacheMap.TryGetValue(key, out LinkedListNode<CacheItem<K, V>> node))
                {
                    value = node.Value.value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                    return true;
                }
                else
                {
                    value = default(V);
                    return false;
                }
            }
        }

        public void TryAdd(K key, V val)
        {
            lock (l)
            {
                if (cacheMap.ContainsKey(key))
                {
                    return;
                }
                Add(key, val);
            }
        }

        public void Add(K key, V val)
        {
            lock (l)
            {
                if (cacheMap.Count >= capacity)
                {
                    RemoveFirst();
                }

                CacheItem<K, V> cacheItem = new CacheItem<K, V>(key, val);
                LinkedListNode<CacheItem<K, V>> node = new LinkedListNode<CacheItem<K, V>>(cacheItem);
                lruList.AddLast(node);
                cacheMap.Add(key, node);
            }
        }

        public void AddOrUpdate(K key, V value)
        {
            lock (l)
            {
                if (cacheMap.TryGetValue(key, out LinkedListNode<CacheItem<K, V>> node))
                {
                    node.Value.value = value;
                    lruList.Remove(node);
                    lruList.AddLast(node);
                }
                else
                {
                    Add(key, value);
                }
            }
        }

        public void TryRemove(K key)
        {
            lock (l)
            {
                if (cacheMap.TryGetValue(key, out var node))
                {
                    cacheMap.Remove(key);
                    lruList.Remove(node);
                }
            }
        }

        public void Clear()
        {
            lock (l)
            {
                cacheMap.Clear();
                lruList.Clear();
            }
        }

        private void RemoveFirst()
        {
            lock (l)
            {
                // Remove from LRUPriority
                LinkedListNode<CacheItem<K, V>> node = lruList.First;
                lruList.RemoveFirst();
                // Remove from cache
                cacheMap.Remove(node.Value.key);
            }
        }
    }
}
