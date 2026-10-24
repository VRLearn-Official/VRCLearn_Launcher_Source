using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace VRCLearn.Core.Collections;

public sealed class OrderedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _values = [];
    private readonly List<TKey> _keys = [];

    public OrderedDictionary()
    {
    }

    public OrderedDictionary(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        foreach (var item in items)
        {
            this[item.Key] = item.Value;
        }
    }

    public int Count => _keys.Count;

    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => _keys.AsReadOnly();

    public ICollection<TValue> Values => _keys.Select(key => _values[key]).ToList().AsReadOnly();

    IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

    IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

    public TValue this[TKey key]
    {
        get => _values[key];
        set
        {
            if (_values.TryAdd(key, value))
            {
                _keys.Add(key);

                return;
            }

            _values[key] = value;
        }
    }

    public void Add(TKey key, TValue value)
    {
        _values.Add(key, value);
        _keys.Add(key);
    }

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

    public bool ContainsKey(TKey key) => _values.ContainsKey(key);

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) =>
        _values.TryGetValue(key, out value);

    public bool Remove(TKey key)
    {
        if (!_values.Remove(key))
        {
            return false;
        }

        _keys.Remove(key);

        return true;
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) =>
        _values.TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(value, item.Value);

    public bool Remove(KeyValuePair<TKey, TValue> item) => Contains(item) && Remove(item.Key);

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        foreach (var item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    public void Clear()
    {
        _values.Clear();
        _keys.Clear();
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        foreach (var key in _keys)
        {
            yield return new KeyValuePair<TKey, TValue>(key, _values[key]);
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
