using System.Diagnostics;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

internal sealed class SnsMessagePublisher(
    IAmazonSimpleNotificationService client,
    IOutboundMessageConverter messageConverter,
    ILoggerFactory loggerFactory,
    Func<Exception, Message, bool> handleException,
    Func<Exception, IReadOnlyCollection<Message>, bool> handleBatchException) : IMessagePublisher, IMessageBatchPublisher, IPreparedBatchPublisher, IInterrogable
{
    private readonly IOutboundMessageConverter _messageConverter = messageConverter;
    private readonly Func<Exception, Message, bool> _handleException = handleException;
    private readonly Func<Exception, IReadOnlyCollection<Message>, bool> _handleBatchException = handleBatchException;
    private readonly IAmazonSimpleNotificationService _client = client;
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }
    public string Arn { get; internal set; }

    public SnsMessagePublisher(
        string topicArn,
        IAmazonSimpleNotificationService client,
        IOutboundMessageConverter messageConverter,
        ILoggerFactory loggerFactory,
        Func<Exception, Message, bool> handleException,
        Func<Exception, IReadOnlyCollection<Message>, bool> handleBatchException)
        : this(client, messageConverter, loggerFactory, handleException, handleBatchException)
    {
        Arn = topicArn;
    }

    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task PublishAsync(Message message, CancellationToken cancellationToken)
        => PublishAsync(message, null, cancellationToken);

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
    {
        var request = await BuildPublishRequestAsync(message, metadata);

        Activity.Current?.SetTag("messaging.system", "aws_sns");
        Activity.Current?.SetTag("messaging.destination.name", request.TopicArn);

        PublishResponse response = null;
        try
        {
            response = await _client.PublishAsync(request, cancellationToken).ConfigureAwait(false);
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

        using (_logger.BeginScope(new Dictionary<string, object> { ["AwsRequestId"] = response?.MessageId }))
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

    private async Task<PublishRequest> BuildPublishRequestAsync(Message message, PublishMetadata metadata)
    {
        var (messageToSend, attributes, subject) = await _messageConverter.ConvertToOutboundMessageAsync(message, metadata);

        var request = new PublishRequest
        {
            TopicArn = Arn,
            Subject = subject,
            Message = messageToSend,
        };

        AddMessageAttributes(request, attributes);

        return request;
    }

    private static void AddMessageAttributes(PublishRequest request, Dictionary<string, Messaging.MessageAttributeValue> attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return;
        }

        request.MessageAttributes ??= [];
        foreach (var attribute in attributes)
        {
            request.MessageAttributes.Add(attribute.Key, BuildMessageAttributeValue(attribute.Value));
        }
    }

    private static void AddMessageAttributes(PublishBatchRequestEntry request, Dictionary<string, Messaging.MessageAttributeValue> attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return;
        }

        request.MessageAttributes ??= [];
        foreach (var attribute in attributes)
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
        return new InterrogationResult(new { Arn });
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        foreach (var batch in await PrepareAsync([.. messages], metadata, cancellationToken).ConfigureAwait(false))
        {
            await batch.SendAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PreparedBatch>> PrepareAsync(IReadOnlyCollection<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        int maxCount = metadata?.BatchSize ?? JustSayingConstants.MaximumSnsBatchSize;
        maxCount = Math.Min(maxCount, JustSayingConstants.MaximumSnsBatchSize);

        // SNS validates the combined size of every entry in the batch against the topic's
        // MaximumMessageSize, so the batch has to be packed by size as well as by count.
        int maxBytes = _messageConverter.MaximumMessageSize;

        Activity.Current?.SetTag("messaging.system", "aws_sns");
        Activity.Current?.SetTag("messaging.destination.name", Arn);

        // Every message is converted before anything is sent, so that a message which cannot be
        // published (one that is too large, say) fails before any of the others have gone out.
        var converted = new List<(Message Message, OutboundMessage Outbound, int Size)>(messages.Count);

        foreach (var message in messages)
        {
            var outbound = await _messageConverter.ConvertToOutboundMessageAsync(message, metadata, cancellationToken);
            converted.Add((message, outbound, MessagePayloadSize.Calculate(outbound.Body, outbound.MessageAttributes)));
        }

        var batches = new List<PreparedBatch>();

        foreach (var batch in MessagePayloadSize.Pack(converted, static x => x.Size, maxCount, maxBytes))
        {
            batches.Add(new PreparedBatch(
                [.. batch.Select(static x => x.Message)],
                token => PublishBatchAsync(batch, token)));
        }

        return batches;
    }

    private async Task PublishBatchAsync(
        List<(Message Message, OutboundMessage Outbound, int Size)> batch,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Message> chunk = [.. batch.Select(static x => x.Message)];

        // The request is built for each attempt, rather than once when the batch was prepared, as a binary
        // attribute is sent as a stream that should not be relied on to be readable a second time.
        var request = new PublishBatchRequest
        {
            TopicArn = Arn,
            PublishBatchRequestEntries = [.. batch.Select(static x => BuildPublishBatchEntry(x.Message, x.Outbound))],
        };

        PublishBatchResponse response = null;
        try
        {
            response = await _client.PublishBatchAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (AmazonServiceException ex)
        {
            _logger.LogWarning(ex, "Failed to publish batch of messages to SNS topic {TopicArn}.", request.TopicArn);

            if (!ClientExceptionHandler(ex, chunk))
            {
                throw new PublishBatchException($"Failed to publish batch of messages to SNS. Topic ARN: '{request.TopicArn}'.", ex);
            }
        }

        if (response is not null)
        {
            using var scope = _logger.BeginScope(new Dictionary<string, string> { ["AwsRequestId"] = response.ResponseMetadata?.RequestId });

            if (response.Successful is not null && response.Successful.Count > 0 && _logger.IsEnabled(LogLevel.Information))
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

            if (response.Failed is not null && response.Failed.Count > 0 && _logger.IsEnabled(LogLevel.Error))
            {
                _logger.LogError(
                    "Failed to publish batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                    response.Failed.Count,
                    "Topic",
                    request.TopicArn);

                foreach (var message in response.Failed)
                {
                    _logger.LogError(
                        "Failed to publish message {MessageId} to {DestinationType} '{MessageDestination}' with error code: {ErrorCode} is error on BatchAPI: {IsBatchAPIError}.",
                        message.Id,
                        "Topic",
                        request.TopicArn,
                        message.Code,
                        message.SenderFault);
                }
            }
        }

        if (MessageBatchResponseLogger is not null)
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

    private bool ClientExceptionHandler(Exception ex, IReadOnlyCollection<Message> messages)
        => _handleBatchException?.Invoke(ex, messages) ?? false;

    private static PublishBatchRequestEntry BuildPublishBatchEntry(Message message, OutboundMessage outbound)
    {
        PublishBatchRequestEntry entry = new()
        {
            Id = message.UniqueKey(),
            Subject = outbound.Subject,
            Message = outbound.Body,
        };

        AddMessageAttributes(entry, outbound.MessageAttributes);

        return entry;
    }
}
