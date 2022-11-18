using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Extensions;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageSerialization;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;

namespace JustSaying.AwsTools.MessageHandling;

public class SqsMessagePublisher : IMessagePublisher, IMessageBatchPublisher
{
    private readonly IAmazonSQS _client;
    private readonly IMessageSerializationRegister _serializationRegister;
    private readonly ILogger _logger;
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public Action<MessageBatchResponse, IEnumerable<Message>> MessageBatchResponseLogger { get; set; }

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

    public async Task PublishAsync(Message message, CancellationToken cancellationToken)
        => await PublishAsync(message, null, cancellationToken).ConfigureAwait(false);

    public async Task PublishAsync(Message message, PublishMetadata metadata, CancellationToken cancellationToken)
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

    public string GetMessageInContext(Message message) => _serializationRegister.Serialize(message, serializeForSnsPublishing: false);

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            QueueUrl
        });
    }

    public Task PublishAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        => PublishAsync(messages, null, cancellationToken);

    public async Task PublishAsync(IEnumerable<Message> messages, PublishBatchMetadata metadata, CancellationToken cancellationToken)
    {
        if (QueueUrl is null)
        {
            throw new PublishException("Queue URL was null, perhaps you need to call `StartAsync` on the `IMessagePublisher` before publishing.");
        }

        var size = metadata?.BatchSize ?? 10;
        size = Math.Min(size, 10);

        foreach (var chuck in messages.Chunk(size))
        {
            var request = BuildSendMessageBatchRequest(chuck, metadata);
            SendMessageBatchResponse response;
            try
            {
                response = await _client.SendMessageBatchAsync(request, cancellationToken).ConfigureAwait(false);
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
                    if (response.Successful.Count > 0)
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

                    if (response.Failed.Count > 0)
                    {
                        _logger.LogError(
                            "Fail to published batch of {MessageCount} to {DestinationType} '{MessageDestination}'.",
                            response.Failed.Count,
                            "Queue",
                            request.QueueUrl);

                        foreach (var message in response.Failed)
                        {
                            _logger.LogWarning(
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
                    SuccessfulMessageIds = response?.Successful.Select(x => x.MessageId),
                    FailedMessageIds = response?.Failed.Select(x => x.Id),
                    ResponseMetadata = response?.ResponseMetadata,
                    HttpStatusCode = response?.HttpStatusCode,
                };

                MessageBatchResponseLogger(responseData, chuck);
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

                if (metadata?.Delay != null)
                {
                    entry.DelaySeconds = (int)metadata.Delay.Value.TotalSeconds;
                }

                return entry;
            }).ToList()
        };


        return request;
    }
}
