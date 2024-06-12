using System.Text;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling;

public class SqsMessagePublisher(
    IAmazonSQS client,
    IMessageSerializationRegister serializationRegister,
    ILoggerFactory loggerFactory) : IMessagePublisher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public PublishCompressionOptions CompressionOptions { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory) : this(client, serializationRegister, loggerFactory)
    {
        QueueUrl = queueUrl;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        if (QueueUrl is null) throw new PublishException("Queue URL was null, perhaps you need to call `StartAsync` on the `IMessagePublisher` before publishing.");

        var request = BuildSendMessageRequest(message, metadata);
        SendMessageResponse response;
        try
        {
            response = await client.SendMessageAsync(request, cancellationToken).ConfigureAwait(false);
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
                message.Id,
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

    private SendMessageRequest BuildSendMessageRequest(Message message, PublishMetadata metadata)
    {
        var messageBody = GetMessageInContext(message);

        var bodyByteLength = Encoding.UTF8.GetByteCount(messageBody); // We should probably also include the length of the message attributes
        if (bodyByteLength > CompressionOptions.MaxMessageSize) // Well under 256KB
        {
            // compress the message body
        }

        var request = new SendMessageRequest
        {
            MessageBody = messageBody,
            QueueUrl = QueueUrl.AbsoluteUri,
        };

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int) metadata.Delay.Value.TotalSeconds;
        }

        return request;
    }

    public string GetMessageInContext(Message message) => serializationRegister.Serialize(message, serializeForSnsPublishing: false);

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            QueueUrl
        });
    }
}

public sealed class PublishCompressionOptions
{
    public static PublishCompressionOptions Default => new();

    public int MaxMessageSize { get; set; } = 248 * 1024; // 256KB - 8KB for overhead
}
