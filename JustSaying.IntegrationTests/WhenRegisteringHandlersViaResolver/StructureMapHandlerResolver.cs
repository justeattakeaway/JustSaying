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

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            var handler = _container.TryGetInstance<IHandlerAsync<T>>();
            if (handler != null)
            {
                return handler;
            }

            var syncHandler = _container.TryGetInstance<IHandler<T>>();
            if (syncHandler != null)
            {
                return new BlockingHandler<T>(syncHandler);
            }

            return null;
        }
    }
}
