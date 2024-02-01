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
    ILoggerFactory loggerFactory) : IMessagePublisher, IMessageBatchPublisher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IReadOnlyCollection<Message>> MessageBatchResponseLogger { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        IMessageSerializationRegister serializationRegister,
        ILoggerFactory loggerFactory) : this(client, serializationRegister, loggerFactory)
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

    private SendMessageRequest BuildSendMessageRequest(Message message, PublishMetadata metadata)
    {
        var request = new SendMessageRequest
        {
            MessageBody = GetMessageInContext(message),
            QueueUrl = QueueUrl.AbsoluteUri,
        };

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
        }

        return request;
    }

    public string GetMessageInContext(Message message) => serializationRegister.Serialize(message, serializeForSnsPublishing: false);

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
            var request = BuildSendMessageBatchRequest(chunk, metadata);
            SendMessageBatchResponse response;
            try
            {
                response = await client.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (AmazonServiceException ex)
            {
                throw new PublishBatchException(
                    $"Failed to publish batch message to SQS. {nameof(request.QueueUrl)}: {request.QueueUrl}",
                    ex);
            }

            if (response != null)
            {
                using (_logger.BeginScope(new Dictionary<string, string>
                {
                    ["AwsRequestId"] = response.ResponseMetadata?.RequestId
                }))
                {
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
                            "Fail to published batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                            response.Failed.Count,
                            "Queue",
                            request.QueueUrl);

                        foreach (var message in response.Failed)
                        {
                            _logger.LogError(
                                "Fail to published message {MessageId} to {DestinationType} '{MessageDestination}' with error code: {ErrorCode} is error on BatchAPI: {IsBatchAPIError}.",
                                message.Id,
                                "Queue",
                                request.QueueUrl,
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
                    SuccessfulMessageIds = response?.Successful.Select(x => x.MessageId).ToArray(),
                    FailedMessageIds = response?.Failed.Select(x => x.Id).ToArray(),
                    ResponseMetadata = response?.ResponseMetadata,
                    HttpStatusCode = response?.HttpStatusCode,
                };

                MessageBatchResponseLogger(responseData, chunk);
            }
        }
    }

    private SendMessageBatchRequest BuildSendMessageBatchRequest(Message[] message, PublishMetadata metadata)
    {
        var request = new SendMessageBatchRequest
        {
            QueueUrl = QueueUrl.AbsoluteUri,
            Entries = message.Select(message =>
            {
                var entry = new SendMessageBatchRequestEntry
                {
                    Id = message.UniqueKey(),
                    MessageBody = GetMessageInContext(message)
                };

                if (metadata?.Delay is { } delay)
                {
                    entry.DelaySeconds = (int)delay.TotalSeconds;
                }

                return entry;
            }).ToList()
        };

        return request;
    }

    private void EnsureQueueUrl()
    {
        if (QueueUrl is null)
        {
            throw new PublishException($"Queue URL was null. Perhaps you need to call the ${nameof(IMessagePublisher.StartAsync)} method on the ${nameof(IMessagePublisher)} before publishing.");
        }
    }
}
