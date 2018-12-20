using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
{
    class HandlerWithMessageContext : IHandlerAsync<OrderPlaced>
    {
        private readonly IMessageContextReader _contextReader;

        public HandlerWithMessageContext(
            Future<OrderPlaced> future,
            IMessageContextReader contextReader)
        {
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

            Console.WriteLine($"Message context found with queue {context.QueueUri}");
            Console.WriteLine($"And message body {context.Message.Body}");

            await Future.Complete(message);
            return true;
        }
    }
}
