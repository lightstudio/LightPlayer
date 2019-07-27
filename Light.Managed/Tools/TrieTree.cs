using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Managed.Tools
{
    public static class TrieTreeExtensions
    {
        public static TrieTreeNode<TKey, TValue> ToTrieTree<TKey, TValue>(
            this IEnumerable<TValue> source,
            Func<TValue, IEnumerable<TKey>> keySelector,
            IEqualityComparer<TKey> comparer)
        {
            var tree = new TrieTreeNode<TKey, TValue>(comparer);
            foreach (var value in source)
            {
                tree.Insert(keySelector(value).GetEnumerator(), value);
            }
            return tree;
        }
    }

    public class TrieTreeNode<TKey, TValue>
    {
        private bool _hasKeyword;
        private TKey _keyword;
        private List<TValue> _values;
        private int _childrenValueCount;
        private IEqualityComparer<TKey> _comparer;

        private Dictionary<TKey, TrieTreeNode<TKey, TValue>> _children;

        public TKey Keyword
        {
            get
            {
                if (_hasKeyword)
                {
                    return _keyword;
                }
                else
                {
                    throw new InvalidOperationException("No keyword on this node.");
                }
            }
        }

        public int ChildrenValueCount => _childrenValueCount;

        public TrieTreeNode(IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            _keyword = default(TKey);
            _hasKeyword = false;
            _childrenValueCount = 0;
            _children = new Dictionary<TKey, TrieTreeNode<TKey, TValue>>(comparer);
        }

        public TrieTreeNode(TKey keyword, IEqualityComparer<TKey> comparer)
        {
            _comparer = comparer;
            _keyword = keyword;
            _hasKeyword = true;
            _childrenValueCount = 0;
            _children = new Dictionary<TKey, TrieTreeNode<TKey, TValue>>(comparer);
        }

        public IEnumerable<TValue> GetChildValues()
        {
            if (_values != null)
            {
                foreach (var value in _values)
                {
                    yield return value;
                }
            }
            foreach (var child in _children.Values)
            {
                foreach (var r in child.GetChildValues())
                {
                    yield return r;
                }
            }
        }

        public TrieTreeNode<TKey, TValue> Lookup(IEnumerator<TKey> keyword)
        {
            if (keyword.MoveNext())
            {
                var current = keyword.Current;
                if (_children.TryGetValue(current, out var node))
                {
                    return node.Lookup(keyword);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return this;
            }
        }

        public void Insert(IEnumerator<TKey> keyword, TValue value)
        {
            if (keyword.MoveNext())
            {
                var current = keyword.Current;
                if (_children.TryGetValue(current, out var node))
                {
                    node.Insert(keyword, value);
                }
                else
                {
                    var child = new TrieTreeNode<TKey, TValue>(current, _comparer);
                    child.Insert(keyword, value);
                    _children.Add(current, child);
                }
            }
            else
            {
                if (_values == null)
                {
                    _values = new List<TValue>();
                }
                _values.Add(value);
            }
            _childrenValueCount += 1;
        }

        public int Remove(IEnumerator<TKey> keyword)
        {
            if (keyword.MoveNext())
            {
                var current = keyword.Current;
                if (_children.TryGetValue(current, out var node))
                {
                    var ret = node.Remove(keyword);
                    if (node.ChildrenValueCount == 0)
                    {
                        _children.Remove(current);
                    }
                    _childrenValueCount -= ret;
                    return ret;
                }
                else
                {
                    throw new InvalidOperationException("Keyword does not exist.");
                }
            }
            else
            {
                if (_values != null)
                {
                    var ret = _values.Count;
                    _values.Clear();
                    _childrenValueCount -= ret;
                    return ret;
                }
                else
                {
                    return 0;
                }
            }
        }
    }
}
