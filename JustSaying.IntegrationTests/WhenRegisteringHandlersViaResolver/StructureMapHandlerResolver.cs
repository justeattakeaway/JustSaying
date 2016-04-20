using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class StructureMapHandlerResolver : IHandlerResolver
    {
        private readonly IContainer _container;

        public StructureMapHandlerResolver(IContainer container)
        {
            _container = container;
        }

        public IEnumerable<IHandlerAsync<T>> ResolveHandlers<T>()
        {
            return _container.GetAllInstances<IHandlerAsync<T>>();
        }
    }
}