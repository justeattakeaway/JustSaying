using System;
using System.Linq;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class HandlerMetadata
    {
        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandlerAsync<T> handler) where T : Message
        {
            var asyncingHandler = handler as BlockingHandler<T>;
            if (asyncingHandler != null)
            {
                return ReadExactlyOnce(asyncingHandler.Inner);
            }

            return new ExactlyOnceReader(handler.GetType());
        }

#pragma warning disable 618
        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandler<T> handler) where T : Message
        {
            return new ExactlyOnceReader(handler.GetType());
        }
#pragma warning restore 618
    }

    internal class ExactlyOnceReader
    {
        private const int DefaultTemporaryLockSeconds = 30;
        private readonly Type _type;

        public ExactlyOnceReader(Type type)
        {
            _type = type;
        }

        public bool Enabled
        {
            get { return Attribute.IsDefined(_type, typeof(ExactlyOnceAttribute)); }
        }

        public int GetTimeOut()
        {
            var attributes = _type.GetCustomAttributes(true);
            var targetAttribute = attributes.FirstOrDefault(a => a is ExactlyOnceAttribute);

            if (targetAttribute != null)
            {
                var exactlyOnce = (ExactlyOnceAttribute)targetAttribute;
                return exactlyOnce.TimeOut;
            }

            return DefaultTemporaryLockSeconds;
        }
    }
}