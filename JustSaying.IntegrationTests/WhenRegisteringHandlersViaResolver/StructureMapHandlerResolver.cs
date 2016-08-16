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

        public IHandlerAsync<T> ResolveHandler<T>()
        {
            var handler = _container.GetInstance<IHandlerAsync<T>>();
            if (handler != null)
            {
                return handler;
            }

            var syncHandler = _container.GetInstance<IHandler<T>>();
            if (syncHandler != null)
            {
                return new BlockingHandler<T>(syncHandler);
            }

            return null;
        }
    }
}