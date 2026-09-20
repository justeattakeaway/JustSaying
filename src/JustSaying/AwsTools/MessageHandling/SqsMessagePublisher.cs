using System.Diagnostics;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

internal sealed class SqsMessagePublisher(
    IAmazonSQS client,
    OutboundMessageConverter messageConverter,
    ILoggerFactory loggerFactory) : IMessagePublisher, IMessageBatchPublisher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        OutboundMessageConverter messageConverter,
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

        Activity.Current?.SetTag("messaging.system", "aws_sqs");
        Activity.Current?.SetTag("messaging.destination.name", request.QueueUrl);

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
        var (messageBody, attributes, _) = await messageConverter.ConvertToOutboundMessageAsync(message, metadata);

        var request = new SendMessageRequest
        {
            MessageBody = messageBody,
            QueueUrl = QueueUrl.AbsoluteUri
        };

        AddMessageAttributes(request, attributes);

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
        }

        return request;
    }

    private static void AddMessageAttributes(SendMessageRequest request, Dictionary<string, Messaging.MessageAttributeValue> messageAttributes)
    {
        if (messageAttributes == null || messageAttributes.Count == 0)
        {
            return;
        }

        request.MessageAttributes ??= [];
        foreach (var attribute in messageAttributes)
        {
            request.MessageAttributes.Add(attribute.Key, BuildMessageAttributeValue(attribute.Value));
        }
    }

    private static void AddMessageAttributes(SendMessageBatchRequestEntry request, Dictionary<string, Messaging.MessageAttributeValue> messageAttributes)
    {
        if (messageAttributes == null || messageAttributes.Count == 0)
        {
            return;
        }

        request.MessageAttributes ??= [];
        foreach (var attribute in messageAttributes)
        {
            request.MessageAttributes.Add(attribute.Key, BuildMessageAttributeValue(attribute.Value));
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

        int maxCount = metadata?.BatchSize ?? JustSayingConstants.MaximumSqsBatchSize;
        maxCount = Math.Min(maxCount, JustSayingConstants.MaximumSqsBatchSize);

        // SQS validates the combined size of every entry in the batch against the queue's
        // maximum message size, so the batch has to be packed by size as well as by count.
        int maxBytes = messageConverter.MaximumMessageSize;

        Activity.Current?.SetTag("messaging.system", "aws_sqs");
        Activity.Current?.SetTag("messaging.destination.name", QueueUrl?.AbsoluteUri);

        var entries = new List<SendMessageBatchRequestEntry>(maxCount);
        var batched = new List<Message>(maxCount);
        int batchBytes = 0;

        foreach (var message in messages)
        {
            var (entry, entrySize) = await BuildSendMessageBatchEntryAsync(message, metadata);

            if (entries.Count > 0 && (entries.Count >= maxCount || batchBytes + entrySize > maxBytes))
            {
                await SendBatchAsync(entries, batched, cancellationToken).ConfigureAwait(false);
                entries = new List<SendMessageBatchRequestEntry>(maxCount);
                batched = new List<Message>(maxCount);
                batchBytes = 0;
            }

            entries.Add(entry);
            batched.Add(message);
            batchBytes += entrySize;
        }

        if (entries.Count > 0)
        {
            await SendBatchAsync(entries, batched, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task SendBatchAsync(
        List<SendMessageBatchRequestEntry> entries,
        IReadOnlyCollection<Message> chunk,
        CancellationToken cancellationToken)
    {
        var request = new SendMessageBatchRequest
        {
            QueueUrl = QueueUrl.AbsoluteUri,
            Entries = entries,
        };

        SendMessageBatchResponse response;
        try
        {
            response = await client.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonServiceException ex)
        {
            throw new PublishBatchException(
                $"Failed to publish batch of {chunk.Count} messages to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl}",
                ex);
        }

        if (response != null)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, string> { ["AwsRequestId"] = response.ResponseMetadata?.RequestId });
            if (response.Successful is not null && response.Successful.Count > 0 && _logger.IsEnabled(LogLevel.Information))
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

            if (response.Failed is not null && response.Failed.Count > 0 && _logger.IsEnabled(LogLevel.Error))
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
                SuccessfulMessageIds = response?.Successful?.Select(x => x.MessageId).ToArray(),
                FailedMessageIds = response?.Failed?.Select(x => x.Id).ToArray(),
                ResponseMetadata = response?.ResponseMetadata,
                HttpStatusCode = response?.HttpStatusCode,
            };

            MessageBatchResponseLogger(responseData, chunk);
        }
    }

    private async Task<(SendMessageBatchRequestEntry Entry, int Size)> BuildSendMessageBatchEntryAsync(Message message, PublishMetadata metadata)
    {
        var (messageBody, attributes, _) = await messageConverter.ConvertToOutboundMessageAsync(message, metadata);

        var entry = new SendMessageBatchRequestEntry
        {
            Id = message.UniqueKey(),
            MessageBody = messageBody
        };

        AddMessageAttributes(entry, attributes);

        if (metadata?.Delay is { } delay)
        {
            entry.DelaySeconds = (int)delay.TotalSeconds;
        }

        return (entry, MessagePayloadSize.Calculate(messageBody, attributes));
    }

    private void EnsureQueueUrl()
    {
        if (QueueUrl is null)
        {
            throw new PublishException($"Queue URL was null. Perhaps you need to call the ${nameof(IMessagePublisher.StartAsync)} method on the ${nameof(IMessagePublisher)} before publishing.");
        }
    }
}
