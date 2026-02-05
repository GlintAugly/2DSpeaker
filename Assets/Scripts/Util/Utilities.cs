public class Utilities
{
    public static V GetDictionaryValueOrDefault<V, K>(System.Collections.Generic.Dictionary<K, V> dict, K key, V defaultValue)
    {
        if (dict.TryGetValue(key, out V value))
        {
            return value;
        }
        return defaultValue;
    }

}
