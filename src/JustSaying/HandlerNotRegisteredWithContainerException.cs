#if !NET8_0_OR_GREATER
using System.Runtime.Serialization;
#endif

namespace JustSaying;

#if !NET8_0_OR_GREATER
[Serializable]
#endif
public class HandlerNotRegisteredWithContainerException : Exception
{
    public HandlerNotRegisteredWithContainerException() : base("Handler not registered with container")
    {
    }

    public HandlerNotRegisteredWithContainerException(string message) : base(message)
    {
    }

    public HandlerNotRegisteredWithContainerException(string message, Exception inner) : base(message, inner)
    {
    }

#if !NET8_0_OR_GREATER
    protected HandlerNotRegisteredWithContainerException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
#endif
}
