using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

internal sealed class SqsMessagePublisher(
    IAmazonSQS client,
    PublishMessageConverter messageConverter,
    ILoggerFactory loggerFactory) : IMessagePublisher, IMessageBatchPublisher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        PublishMessageConverter messageConverter,
        ILoggerFactory loggerFactory) : this(client, messageConverter, loggerFactory)
    {
        QueueUrl = queueUrl;
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    /// <inheritdoc/>
    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        EnsureQueueUrl();

        var request = await BuildSendMessageRequestAsync(message, metadata);
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

        using (_logger.BeginScope(new Dictionary<string, string> { ["AwsRequestId"] = response?.MessageId }))
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

    private async Task<SendMessageRequest> BuildSendMessageRequestAsync(Message message, PublishMetadata metadata)
    {
        var (messageBody, attributes, _) = await messageConverter.ConvertForPublishAsync(message, metadata);

        var request = new SendMessageRequest
        {
            MessageBody = messageBody,
            QueueUrl = QueueUrl.AbsoluteUri
        };

        AddMessageAttributes(request.MessageAttributes, attributes);

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
        }

        return request;
    }

    private static void AddMessageAttributes(Dictionary<string, MessageAttributeValue> requestMessageAttributes, Dictionary<string, Messaging.MessageAttributeValue> messageAttributes)
    {
        if (messageAttributes == null || messageAttributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in messageAttributes)
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

    /// <inheritdoc/>
    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            QueueUrl
        });
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        EnsureQueueUrl();

        int size = metadata?.BatchSize ?? JustSayingConstants.MaximumSnsBatchSize;
        size = Math.Min(size, JustSayingConstants.MaximumSnsBatchSize);

        foreach (var chunk in messages.Chunk(size))
        {
            var request = await BuildSendMessageBatchRequestAsync(chunk, metadata);
            SendMessageBatchResponse response;
            try
            {
                response = await client.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException ex)
            {
                throw new PublishBatchException(
                    $"Failed to publish batch of {chunk.Length} messages to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl}",
                    ex);
            }

            if (response != null)
            {
                using var scope = _logger.BeginScope(new Dictionary<string, string> { ["AwsRequestId"] = response.ResponseMetadata?.RequestId });
                if (response.Successful.Count > 0 && _logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "Published batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                        response.Successful.Count,
                        "Queue",
                        request.QueueUrl);

                    foreach (var message in response.Successful)
                    {
                        _logger.LogInformation(
                            "Published message {MessageId} of type {MessageType} to {DestinationType} '{MessageDestination}'.",
                            message.Id,
                            message.GetType().FullName,
                            "Queue",
                            request.QueueUrl);
                    }
                }

                if (response.Failed.Count > 0 && _logger.IsEnabled(LogLevel.Error))
                {
                    _logger.LogError(
                        "Failed to publish batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                        response.Failed.Count,
                        "Queue",
                        request.QueueUrl);

                    foreach (var message in response.Failed)
                    {
                        _logger.LogError(
                            "Failed to publish message {MessageId} to {DestinationType} '{MessageDestination}' with error code: {ErrorCode} is error on BatchAPI: {IsBatchAPIError}.",
                            message.Id,
                            "Queue",
                            request.QueueUrl,
                            message.Code,
                            message.SenderFault);
                    }
                }
            }

            if (MessageBatchResponseLogger != null)
            {
                var responseData = new MessageBatchResponse
                {
                    SuccessfulMessageIds = response?.Successful.Select(x => x.MessageId).ToArray(),
                    FailedMessageIds = response?.Failed.Select(x => x.Id).ToArray(),
                    ResponseMetadata = response?.ResponseMetadata,
                    HttpStatusCode = response?.HttpStatusCode,
                };

                MessageBatchResponseLogger(responseData, chunk);
            }
        }
    }

    private async Task<SendMessageBatchRequest> BuildSendMessageBatchRequestAsync(Message[] messages, PublishMetadata metadata)
    {
        var entries = new List<SendMessageBatchRequestEntry>(messages.Length);
        int? delaySeconds = metadata?.Delay is { } delay ? (int)delay.TotalSeconds : null;

        foreach (var message in messages)
        {
            var (messageBody, attributes, _) = await messageConverter.ConvertForPublishAsync(message, metadata);

            var entry = new SendMessageBatchRequestEntry
            {
                Id = message.UniqueKey(),
                MessageBody = messageBody
            };

            AddMessageAttributes(entry.MessageAttributes, attributes);

            if (delaySeconds is { } value)
            {
                entry.DelaySeconds = value;
            }

            entries.Add(entry);
        }

        return new SendMessageBatchRequest
        {
            QueueUrl = QueueUrl.AbsoluteUri,
            Entries = entries,
        };
    }

    private void EnsureQueueUrl()
    {
        if (QueueUrl is null)
        {
            throw new PublishException($"Queue URL was null. Perhaps you need to call the ${nameof(IMessagePublisher.StartAsync)} method on the ${nameof(IMessagePublisher)} before publishing.");
        }
    }
}
