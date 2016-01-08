namespace JustSaying.AwsTools.QueueCreation
{
    public interface IRegionResourceCache<T>
    {
        T TryGetFromCache(string region, string key);
        void AddToCache(string region, string key, T value);
    }
}