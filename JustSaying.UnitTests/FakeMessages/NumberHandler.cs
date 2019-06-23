using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.FakeMessages
{
    public class NumberHandler : IHandlerAsync<NumberMessage>
    {
        public NumberHandler(ILogger<NumberHandler> logger)
        {
            Logger = logger;
        }

        private ILogger Logger { get; }

        public Task<bool> Handle(NumberMessage message)
        {
            Logger.LogInformation("Handling message with number {Number}.", message.Number);
            return Task.FromResult(true);
        }
    }
}
