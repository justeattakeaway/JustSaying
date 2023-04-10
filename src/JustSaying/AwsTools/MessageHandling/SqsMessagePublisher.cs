using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling;

public class SqsMessagePublisher<TMessage> : IMessagePublisher<TMessage>  where TMessage : class
{
    private readonly IAmazonSQS _client;
    private readonly IMessageSerializationRegister _serializationRegister;
    private readonly ILogger _logger;
    public Action<MessageResponse, TMessage> MessageResponseLogger { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory) : this(client, serializationRegister, loggerFactory)
    {
        QueueUrl = queueUrl;
    }

    public SqsMessagePublisher(
        IAmazonSQS client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory)
    {
        _client = client;
        _serializationRegister = serializationRegister;
        _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task PublishAsync(TMessage message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    public async Task PublishAsync(TMessage message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        if (QueueUrl is null) throw new PublishException("Queue URL was null, perhaps you need to call `StartAsync` on the `IMessagePublisher` before publishing.");

        var request = BuildSendMessageRequest(message, metadata);
        SendMessageResponse response;
        try
        {
            response = await _client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonServiceException ex)
        {
            throw new PublishException(
                $"Failed to publish message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl},{nameof(request.MessageBody)}: {request.MessageBody}",
                ex);
        }

        using (_logger.BeginScope(new Dictionary<string, object>
               {
                   ["AwsRequestId"] = response?.MessageId
               }))
        {
            _logger.LogInformation(
                "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.",
                (message as Message)?.Id.ToString() ?? "<unknown>",
                message.GetType().FullName,
                "Queue",
                request.QueueUrl);
        }

        if (MessageResponseLogger != null)
        {
            var responseData = new MessageResponse
            {
                HttpStatusCode = response?.HttpStatusCode,
                MessageId = response?.MessageId,
                ResponseMetadata = response?.ResponseMetadata
            };
            MessageResponseLogger.Invoke(responseData, message);
        }
    }

    private SendMessageRequest BuildSendMessageRequest(TMessage message, PublishMetadata metadata)
    {
        var request = new SendMessageRequest
        {
            MessageBody = GetMessageInContext(message),
            QueueUrl = QueueUrl.AbsoluteUri,
        };

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int) metadata.Delay.Value.TotalSeconds;
        }

        return request;
    }

    public string GetMessageInContext(TMessage message) => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            QueueUrl
        });
    }
}
