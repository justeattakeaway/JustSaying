using System.Collections.Generic;
using System.Linq;
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
            var proposedHandlers = GetAllInstances<IHandlerAsync<T>>();
            var proposedSyncHandlers = GetAllInstances<IHandler<T>>()
                .Select(h => new BlockingHandler<T>(h));

            return proposedHandlers.Concat(proposedSyncHandlers);
        }

        private IEnumerable<T> GetAllInstances<T>()
        {
            return _container.GetAllInstances<T>();
        }
    }
}