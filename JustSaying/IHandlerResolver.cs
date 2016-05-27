using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying
{
    public interface IHandlerResolver
    {
        IEnumerable<IHandlerAsync<T>> ResolveHandlers<T>();
    }
}