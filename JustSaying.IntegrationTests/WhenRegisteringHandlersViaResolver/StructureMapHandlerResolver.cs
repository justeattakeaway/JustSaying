using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class StructureMapHandlerResolver : IHandlerResolver
    {
        public IEnumerable<IHandler<T>> ResolveHandlers<T>()
        {
            return ObjectFactory.GetAllInstances<IHandler<T>>();
        }
    }
}