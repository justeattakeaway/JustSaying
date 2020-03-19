using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Factory
{
    internal interface IMultiplexerFactory
    {
        IMultiplexer Create(int channelCapacity);
    }

    internal class MultiplexerFactory : IMultiplexerFactory
    {
        private readonly ILoggerFactory _loggerFactory;

        public MultiplexerFactory(ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;
        }

        public IMultiplexer Create(int channelCapacity)
        {
            return new RoundRobinQueueMultiplexer(channelCapacity,
                _loggerFactory.CreateLogger<RoundRobinQueueMultiplexer>());
        }
    }
}
