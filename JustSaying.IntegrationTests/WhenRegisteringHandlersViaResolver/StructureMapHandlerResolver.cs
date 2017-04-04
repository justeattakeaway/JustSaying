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
            var handler = _container.GetInstance<IHandlerAsync<T>>();
            if (handler != null)
            {
                return handler;
            }

            // we use the obsolete interface"IHandler<T>" here
            #pragma warning disable 618
            var syncHandler = _container.GetInstance<IHandler<T>>();
            if (syncHandler != null)
            {
                return new BlockingHandler<T>(syncHandler);
            }
            #pragma warning restore 618

            return null;
        }
    }
}