#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace JustSaying.AwsTools.QueueCreation;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
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

#if !NET8_0_OR_GREATER
    protected ConfigurationErrorsException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
}
