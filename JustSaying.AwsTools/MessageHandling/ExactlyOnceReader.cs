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
            return asyncingHandler != null ? ReadExactlyOnce(asyncingHandler.Inner) : new ExactlyOnceReader(handler.GetType());
        }

        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandler<T> handler) where T : Message
            => new ExactlyOnceReader(handler.GetType());
    }

    internal class ExactlyOnceReader
    {
        private const int DefaultTemporaryLockSeconds = 30;
        private readonly Type _type;

        public ExactlyOnceReader(Type type)
        {
            _type = type;
        }

        public bool Enabled => Attribute.IsDefined(_type, typeof(ExactlyOnceAttribute));

        public int GetTimeOut()
            => _type
            .GetCustomAttributes(true)
            .OfType<ExactlyOnceAttribute>()
            .FirstOrDefault()?.TimeOut
            ?? DefaultTemporaryLockSeconds;
    }
}