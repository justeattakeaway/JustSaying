using System;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    internal class HandlerWithMessageContext : IHandlerAsync<SimpleMessage>
    {
        private readonly ILogger<HandlerWithMessageContext> _outputHelper;
        private readonly IMessageContextReader _messageContextReader;

        public HandlerWithMessageContext(
            IMessageContextReader messageContextReader,
            Future<SimpleMessage> future,
            ILogger<HandlerWithMessageContext> outputHelper)
        {
            _messageContextReader = messageContextReader;
            Future = future;
            _outputHelper = outputHelper;
        }

        public Future<SimpleMessage> Future { get; }

        public async Task<bool> Handle(SimpleMessage message)
        {
            var messageContext = _messageContextReader.MessageContext;

            if (messageContext == null)
            {
                throw new InvalidOperationException("Message context was not ");
            }

            _outputHelper.LogInformation($"Message context found with queue uri {messageContext.QueueUri}");
            _outputHelper.LogInformation($"And message body {messageContext.Message.Body}");

            await Future.Complete(message);
            return true;
        }
    }
}
