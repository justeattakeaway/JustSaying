using System;
using System.Web.Caching;

namespace JustEat.Simples.Common.Services
{
    public class InProcCacheService : ICacheService
    {
        private readonly Cache _cache;

        public InProcCacheService(Cache cache)
        {
            _cache = cache;
        }

        public T Get<T>(string key) where T : class
        {
            return _cache[key] as T;
        }

        public void InsertWithTimeout<T>(string key, T value, int timeoutSeconds) where T : class
        {
            _cache.Insert(key, value, null, DateTime.UtcNow.Add(TimeSpan.FromSeconds(timeoutSeconds)), Cache.NoSlidingExpiration);
        }

        public void Insert<T>(string key, T value) where T : class
        {
            _cache.Insert(key, value);
        }
    }
}