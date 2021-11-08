using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

internal class HandlerWithMessageContext : IHandlerAsync<SimpleMessage>
{
    private readonly ILogger<HandlerWithMessageContext> _logger;
    private readonly IMessageContextReader _messageContextReader;

    public HandlerWithMessageContext(
        IMessageContextReader messageContextReader,
        Future<SimpleMessage> future,
        ILogger<HandlerWithMessageContext> logger)
    {
        _messageContextReader = messageContextReader;
        Future = future;
        _logger = logger;
    }

    public Future<SimpleMessage> Future { get; }

    public async Task<bool> Handle(SimpleMessage message)
    {
        var messageContext = _messageContextReader.MessageContext;

        if (messageContext == null)
        {
            throw new InvalidOperationException("Message context was not found");
        }

        _logger.LogInformation(
            "Message context found with queue URI {QueueUri} and message body {MessageBody}.",
            messageContext.QueueUri,
            messageContext.Message.Body);

        await Future.Complete(message);
        return true;
    }
}