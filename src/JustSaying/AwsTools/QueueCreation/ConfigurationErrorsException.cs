using System.Runtime.Serialization;

namespace JustSaying.AwsTools.QueueCreation;

[Serializable]
public class ConfigurationErrorsException : Exception
{
    public ConfigurationErrorsException() : base("Invalid configuration")
    {
    }

    public ConfigurationErrorsException(string message) : base(message)
    {
    }

    public ConfigurationErrorsException(string message, Exception inner) : base(message, inner)
    {
    }

    protected ConfigurationErrorsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}