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
            => _container.GetInstance<IHandlerAsync<T>>();
    }
}
