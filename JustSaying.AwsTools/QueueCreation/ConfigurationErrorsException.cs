using System;

namespace JustSaying.AwsTools.QueueCreation
{
    [Serializable]
    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException()
        {
        }

        public ConfigurationErrorsException(string message) : base(message)
        {
        }
    }
}