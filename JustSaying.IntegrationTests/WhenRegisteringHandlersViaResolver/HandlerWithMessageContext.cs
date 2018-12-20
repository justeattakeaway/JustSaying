using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    internal class HandlerWithMessageContext : IHandlerAsync<OrderPlaced>
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly IMessageContextReader _contextReader;

        public HandlerWithMessageContext(
            ITestOutputHelper outputHelper,
            Future<OrderPlaced> future,
            IMessageContextReader contextReader)
        {
            _outputHelper = outputHelper;
            Future = future;
            _contextReader = contextReader;
        }

        public Future<OrderPlaced> Future { get; }

        public async Task<bool> Handle(OrderPlaced message)
        {
            var context = _contextReader.MessageContext;

            if (context == null)
            {
                throw new InvalidOperationException("Message context was not found");
            }

            _outputHelper.WriteLine($"Message context found with queue uri {context.QueueUri}");
            _outputHelper.WriteLine($"And message body {context.Message.Body}");

            await Future.Complete(message);
            return true;
        }
    }
}
