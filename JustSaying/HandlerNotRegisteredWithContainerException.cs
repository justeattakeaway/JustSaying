using System;
using System.Runtime.Serialization;

namespace JustSaying
{
    [Serializable]
    public class HandlerNotRegisteredWithContainerException: Exception
    {
        public HandlerNotRegisteredWithContainerException()
        {
        }

        public HandlerNotRegisteredWithContainerException(string message) : base(message)
        {
        }

        public HandlerNotRegisteredWithContainerException(string message, Exception inner) : base(message, inner)
        {
        }

        protected HandlerNotRegisteredWithContainerException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
