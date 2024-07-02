using System.Text;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

public class SnsMessagePublisher(
    IAmazonSimpleNotificationService client,
    IMessageSerializationRegister serializationRegister,
    ILoggerFactory loggerFactory,
    IMessageSubjectProvider messageSubjectProvider,
    Func<Exception, Message, bool> handleException = null) : IMessagePublisher, IInterrogable
{
    private readonly IMessageSerializationRegister _serializationRegister = serializationRegister;
    private readonly IMessageSubjectProvider _messageSubjectProvider = messageSubjectProvider;
    private readonly Func<Exception, Message, bool> _handleException = handleException;
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public PublishCompressionOptions CompressionOptions { get; set; }
    public IMessageCompressionRegistry CompressionRegistry { get; set; }
    public string Arn { get; internal set; }
    protected IAmazonSimpleNotificationService Client { get; } = client;
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");

    public SnsMessagePublisher(
        string topicArn,
        IAmazonSimpleNotificationService client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory,
        IMessageSubjectProvider messageSubjectProvider,
        Func<Exception, Message, bool> handleException = null)
        : this(client, serializationRegister, loggerFactory, messageSubjectProvider, handleException)
    {
        Arn = topicArn;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        var request = BuildPublishRequest(message, metadata);
        PublishResponse response = null;
        try
        {
            response = await Client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonServiceException ex)
        {
            if (!ClientExceptionHandler(ex, message))
            {
                throw new PublishException(
                    $"Failed to publish message to SNS. Topic ARN: '{request.TopicArn}', Subject: '{request.Subject}', Message: '{request.Message}'.",
                    ex);
            }
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
                "Topic",
                request.TopicArn);
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

    private bool ClientExceptionHandler(Exception ex, Message message) => _handleException?.Invoke(ex, message) ?? false;

    private PublishRequest BuildPublishRequest(Message message, PublishMetadata metadata)
    {
        var messageToSend = _serializationRegister.Serialize(message, serializeForSnsPublishing: true);

        string contentEncoding = null;
        if (CompressionOptions?.CompressionEncoding is { } compressionEncoding && CompressionRegistry is not null)
        {
            var bodyByteLength = Encoding.UTF8.GetByteCount(messageToSend); // We should probably also include the length of the message attributes
            if (bodyByteLength > (CompressionOptions?.MessageLengthThreshold ?? int.MaxValue)) // Well under 256KB
            {
                var compression = CompressionRegistry.GetCompression(compressionEncoding);
                if (compression is null)
                {
                    throw new PublishException($"Compression encoding '{compressionEncoding}' is not registered.");
                }

                messageToSend = compression.Compress(messageToSend);
                contentEncoding = compressionEncoding;
            }
        }

        var messageType = _messageSubjectProvider.GetSubjectForType(message.GetType());

        var request = new PublishRequest
        {
            TopicArn = Arn,
            Subject = messageType,
            Message = messageToSend,
        };

        AddMessageAttributes(request.MessageAttributes, metadata);

        if (contentEncoding is not null)
        {
            request.MessageAttributes.Add("Content-Encoding", new MessageAttributeValue { DataType = "String", StringValue = contentEncoding });
        }

        return request;
    }

    private static void AddMessageAttributes(Dictionary<string, MessageAttributeValue> requestMessageAttributes, PublishMetadata metadata)
    {
        if (metadata?.MessageAttributes == null || metadata.MessageAttributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in metadata.MessageAttributes)
        {
            requestMessageAttributes.Add(attribute.Key, BuildMessageAttributeValue(attribute.Value));
        }
    }

    private static MessageAttributeValue BuildMessageAttributeValue(Messaging.MessageAttributeValue value)
    {
        if (value == null)
        {
            return null;
        }

        var binaryValueStream = value.BinaryValue != null
            ? new MemoryStream([.. value.BinaryValue], false)
            : null;

        return new MessageAttributeValue
        {
            StringValue = value.StringValue,
            BinaryValue = binaryValueStream,
            DataType = value.DataType
        };
    }

    public virtual InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            Arn
        });
    }
}
