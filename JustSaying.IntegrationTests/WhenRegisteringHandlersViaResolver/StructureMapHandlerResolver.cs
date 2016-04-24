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
            var proposedHandlers = _container.GetAllInstances<IHandlerAsync<T>>();

#pragma warning disable 618
            var proposedSyncHandlers = _container.GetAllInstances<IHandler<T>>()
                .Select(h => new AsyncingHandler<T>(h));
#pragma warning restore 618

            return proposedHandlers.Concat(proposedSyncHandlers);
        }
    }
}