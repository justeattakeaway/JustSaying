using System.Collections.Generic;

namespace JustSaying.AwsTools.QueueCreation
{
    public sealed class RegionResourceCache<T>
    {
        private readonly Dictionary<string, Dictionary<string, T>> _regionsData
            = new Dictionary<string, Dictionary<string, T>>();

        public T TryGetFromCache(string region, string key)
        {
            if (! _regionsData.TryGetValue(region, out var regionDict))
            {
                return default;
            }

            if (! regionDict.TryGetValue(key, out var value))
            {
                return default;
            }

            return value;
        }

        public void AddToCache(string region, string key, T value)
        {
            if (!_regionsData.TryGetValue(region, out var regionDict))
            {
                regionDict = new Dictionary<string, T>();
                _regionsData[region] = regionDict;
            }

            regionDict[key] = value;
        }
    }
}
