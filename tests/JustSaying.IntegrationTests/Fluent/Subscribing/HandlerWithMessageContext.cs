using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

internal class HandlerWithMessageContext(
    IMessageContextReader messageContextReader,
    Future<SimpleMessage> future,
    ILogger<HandlerWithMessageContext> logger) : IHandlerAsync<SimpleMessage>
{
    private readonly ILogger<HandlerWithMessageContext> _logger = logger;
    private readonly IMessageContextReader _messageContextReader = messageContextReader;

    public Future<SimpleMessage> Future { get; } = future;

    public async Task<bool> Handle(SimpleMessage message)
    {
        var messageContext = _messageContextReader.MessageContext ?? throw new InvalidOperationException("Message context was not found");
        _logger.LogInformation(
            "Message context found with queue URI {QueueUri} and message body {MessageBody}.",
            messageContext.QueueUri,
            messageContext.Message.Body);

        await Future.Complete(message);
        return true;
    }
}