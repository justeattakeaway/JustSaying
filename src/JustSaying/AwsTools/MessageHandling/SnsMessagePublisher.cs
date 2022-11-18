using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

public class SnsMessagePublisher : IMessagePublisher, IInterrogable, IMessageBatchPublisher
{
    private readonly IMessageSerializationRegister _serializationRegister;
    private readonly IMessageSubjectProvider _messageSubjectProvider;
    private readonly Func<Exception, Message, bool> _handleException;
    private readonly Func<Exception, IEnumerable<Message>, bool> _handleBatchException;
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IEnumerable<Message>> MessageBatchResponseLogger { get; set; }
    public string Arn { get; internal set; }
    protected IAmazonSimpleNotificationService Client { get; }
    private readonly ILogger _logger;

    public SnsMessagePublisher(
        string topicArn,
        IAmazonSimpleNotificationService client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory,
        IMessageSubjectProvider messageSubjectProvider,
        Func<Exception, Message, bool> handleException = null,
        Func<Exception, IEnumerable<Message>, bool> handleBatchException = null)
        : this(client, serializationRegister, loggerFactory, messageSubjectProvider, handleException, handleBatchException)
    {
        Arn = topicArn;
    }

    public SnsMessagePublisher(
        IAmazonSimpleNotificationService client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory,
        IMessageSubjectProvider messageSubjectProvider,
        Func<Exception, Message, bool> handleException = null,
        Func<Exception, IEnumerable<Message>, bool> handleBatchException = null)
    {
        Client = client;
        _serializationRegister = serializationRegister;
        _logger = loggerFactory.CreateLogger("JustSaying.Publish");
        _handleException = handleException;
        _handleBatchException = handleBatchException;
        _messageSubjectProvider = messageSubjectProvider;
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
        var messageType = _messageSubjectProvider.GetSubjectForType(message.GetType());

        return new PublishRequest
        {
            TopicArn = Arn,
            Subject = messageType,
            Message = messageToSend,
            MessageAttributes = BuildMessageAttributes(metadata)
        };
    }

    private static Dictionary<string, MessageAttributeValue> BuildMessageAttributes(PublishMetadata metadata)
    {
        if (metadata?.MessageAttributes == null || metadata.MessageAttributes.Count == 0)
        {
            return null;
        }

        return metadata.MessageAttributes.ToDictionary(
            source => source.Key,
            source => BuildMessageAttributeValue(source.Value));
    }

    private static MessageAttributeValue BuildMessageAttributeValue(Messaging.MessageAttributeValue value)
    {
        if (value == null)
        {
            return null;
        }

        var binaryValueStream = value.BinaryValue != null
            ? new MemoryStream(value.BinaryValue.ToArray(), false)
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

    public Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        => PublishAsync(messages, null, cancellationToken);

    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        var size = metadata?.BatchSize ?? 10;
        size = Math.Min(size, 10);

        foreach (var chuck in messages.Chunk(size))
        {
            var request = BuildPublishBatchRequest(chuck, metadata);
            PublishBatchResponse response = null;
            try
            {
                response = await Client.PublishBatchAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException ex)
            {
                if (!ClientExceptionHandler(ex, chuck))
                {
                    throw new PublishBatchException($"Failed to publish batch of messages to SNS. Topic ARN: '{request.TopicArn}'.", ex);
                }
            }

            if (response is { })
            {
                using (_logger.BeginScope(new Dictionary<string, string>
                       {
                           ["AwsRequestId"] = response.ResponseMetadata?.RequestId
                       }))
                {
                    if (response.Successful.Count > 0)
                    {
                        _logger.LogInformation(
                            "Published batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                            response.Successful.Count,
                            "Topic",
                            request.TopicArn);

                        foreach (var message in response.Successful)
                        {
                            _logger.LogInformation(
                                "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.",
                                message.Id,
                                message.GetType().FullName,
                                "Topic",
                                request.TopicArn);
                        }
                    }

                    if (response.Failed.Count > 0)
                    {
                        _logger.LogError(
                            "Fail to published batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                            response.Failed.Count,
                            "Topic",
                            request.TopicArn);

                        foreach (var message in response.Failed)
                        {
                            _logger.LogWarning(
                                "Fail to published message {MessageId} to {DestinationType} '{MessageDestination}' with error code: {ErrorCode} is error on BatchAPI: {IsBatchAPIError}.",
                                message.Id,
                                "Topic",
                                request.TopicArn,
                                message.Code,
                                message.SenderFault);
                        }
                    }
                }
            }

            if (MessageBatchResponseLogger != null)
            {
                var responseData = new MessageBatchResponse
                {
                    SuccessfulMessageIds = response?.Successful.Select(x => x.MessageId),
                    FailedMessageIds = response?.Failed.Select(x => x.Id),
                    ResponseMetadata = response?.ResponseMetadata,
                    HttpStatusCode = response?.HttpStatusCode,
                };

                MessageBatchResponseLogger(responseData, chuck);
            }
        }
    }

    private bool ClientExceptionHandler(Exception ex, IEnumerable<Message> messages) => _handleBatchException?.Invoke(ex, messages) ?? false;

    private PublishBatchRequest BuildPublishBatchRequest(Message[] message, PublishMetadata metadata)
    {
        return new PublishBatchRequest
        {
            TopicArn = Arn,
            PublishBatchRequestEntries = message.Select(message => new PublishBatchRequestEntry
                {
                    Subject = _messageSubjectProvider.GetSubjectForType(message.GetType()),
                    Message = _serializationRegister.Serialize(message, serializeForSnsPublishing: true),
                    MessageAttributes = BuildMessageAttributes(metadata)
                })
                .ToList()
        };
    }
}
