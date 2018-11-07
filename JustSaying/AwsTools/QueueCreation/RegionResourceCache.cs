using System.Collections.Generic;

namespace JustSaying.AwsTools.QueueCreation
{
    public sealed class RegionResourceCache<T> : IRegionResourceCache<T>
    {
        private readonly Dictionary<string, Dictionary<string, T>> _regionsData
            = new Dictionary<string, Dictionary<string, T>>();

        public T TryGetFromCache(string region, string key)
        {
            if (!_regionsData.ContainsKey(region))
            {
                return default;
            }

            var regionDict = _regionsData[region];
            if (! regionDict.ContainsKey(key))
            {
                return default;
            }

            return regionDict[key];
        }

        public void AddToCache(string region, string key, T value)
        {
            if (!_regionsData.ContainsKey(region))
            {
                _regionsData[region] = new Dictionary<string, T>();
            }
            var regionDict = _regionsData[region];
            regionDict[key] = value;
        }
    }
}
