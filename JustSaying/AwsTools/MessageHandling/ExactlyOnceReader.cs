using System;
using System.Linq;
using System.Reflection;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;

namespace JustSaying.AwsTools.MessageHandling
{
    internal static class HandlerMetadata
    {
        public static ExactlyOnceReader ReadExactlyOnce<T>(IHandlerAsync<T> handler) where T : Message
        {
            return new ExactlyOnceReader(handler.GetType());
        }
    }

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
