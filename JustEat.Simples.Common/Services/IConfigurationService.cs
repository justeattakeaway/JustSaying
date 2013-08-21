namespace JustEat.Simples.Common.Services
{
    public interface IConfigurationService
    {
        string GetString(string key);
        int GetInteger(string key);
        bool GetBoolean(string key);
        string GetConnectionString(string key);
    }
}