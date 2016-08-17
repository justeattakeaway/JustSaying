using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying
{
    public interface IHandlerResolver
    {
        IHandlerAsync<T> ResolveHandler<T>();
    }
}