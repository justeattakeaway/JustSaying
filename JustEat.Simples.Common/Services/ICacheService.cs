namespace JustEat.Simples.Common.Services
{
    public interface ICacheService
    {
        T Get<T>(string key) where T : class;

        void InsertWithTimeout<T>(string key, T value, int timeoutSeconds) where T : class;

        void Insert<T>(string key, T value) where T : class;
    }
}