using System;
using System.Linq;
using System.Reflection;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class HandlerMetadata
    {
        // we use the obsolete interface"IHandler<T>" here
#pragma warning disable 618
        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandlerAsync<T> handler) where T : Message
        {
            var asyncingHandler = handler as BlockingHandler<T>;
            return asyncingHandler != null ? ReadExactlyOnce(asyncingHandler.Inner) : new ExactlyOnceReader(handler.GetType());
        }

        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandler<T> handler) where T : Message
            => new ExactlyOnceReader(handler.GetType());
    }
#pragma warning restore 618

    internal class ExactlyOnceReader
    {
        private const int DefaultTemporaryLockSeconds = 30;
        private readonly Type _type;

        public ExactlyOnceReader(Type type)
        {
            _type = type;
        }

        public bool Enabled => _type.GetTypeInfo().IsDefined(typeof(ExactlyOnceAttribute));

        public int GetTimeOut()
            => _type.GetTypeInfo()
            .GetCustomAttributes(true)
            .OfType<ExactlyOnceAttribute>()
            .FirstOrDefault()?.TimeOut
            ?? DefaultTemporaryLockSeconds;
    }
}