using System;
using System.Configuration;
using System.Globalization;

namespace JustEat.Simples.Common.Services
{
    public class ConfigurationService : IConfigurationService
    {
        public string GetString(string key)
        {
            return GetValue<string>(key);
        }

        public int GetInteger(string key)
        {
            return GetValue<int>(key);
        }

        private static T GetValue<T>(string key)
        {
            var value = ConfigurationManager.AppSettings[key];

            if (value == null)
            {
                throw new ConfigurationErrorsException(
                    string.Format(CultureInfo.InvariantCulture, "Setting for key '{0}' was not found.", key));
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value;
            }

            if (typeof(T) == typeof(int))
            {
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }

            throw new NotImplementedException(
                string.Format(CultureInfo.InvariantCulture, "Type '{0}' is not supported.", typeof(T)));
        }
    }
}