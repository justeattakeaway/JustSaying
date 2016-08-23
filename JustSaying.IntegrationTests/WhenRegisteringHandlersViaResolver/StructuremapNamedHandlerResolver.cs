using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using StructureMap;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    public class TestMessage : Message
    { }

    public class HandlerA : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    public class HandlerB : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    public class HandlerC : IHandlerAsync<TestMessage>
    {
        public Task<bool> Handle(TestMessage message) => Task.FromResult(true);
    }

    public class StructureMapNamedHandlerResolver : IHandlerResolver
    {
        private readonly IContainer _container;

        public StructureMapNamedHandlerResolver()
        {
            _container = new Container(ConfigureContainer);
        }

        private void ConfigureContainer(ConfigurationExpression config)
        {
            config.For<IHandlerAsync<TestMessage>>()
                .Use<HandlerA>().Named("QueueA");

            config.For<IHandlerAsync<TestMessage>>()
                .Use<HandlerB>().Named("QueueB");

            config.For<IHandlerAsync<TestMessage>>()
                .Use<HandlerC>();
        }

        public IHandlerAsync<T> ResolveHandler<T>(HandlerResolutionContext context)
        {
            var namedHandler = _container.TryGetInstance<IHandlerAsync<T>>(context.QueueName);
            if (namedHandler != null)
            {
                return namedHandler;
            }

            return _container.GetInstance<IHandlerAsync<T>>();
        }
    }

}
