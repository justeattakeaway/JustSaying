using System;
using System.Collections.Concurrent;

namespace JustSaying.AwsTools.QueueCreation
{
    public sealed class RegionResourceCache<T>
    {
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, T>> _regionsData
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, T>>(StringComparer.OrdinalIgnoreCase);

        public T TryGetFromCache(string region, string key)
        {
            if (_regionsData.TryGetValue(region, out var regionDict)
                && regionDict.TryGetValue(key, out var value))
            {
                return value;
            }

            return default;
        }

        public void AddToCache(string region, string key, T value)
        {
            var regionDict = _regionsData.GetOrAdd(region,
                r => new ConcurrentDictionary<string, T>(StringComparer.OrdinalIgnoreCase));

            regionDict[key] = value;
        }
    }
}
