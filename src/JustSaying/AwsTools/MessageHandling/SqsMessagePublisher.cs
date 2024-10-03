using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.Interrogation;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;
using Message = JustSaying.Models.Message;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

internal class SqsMessagePublisher(
    IAmazonSQS client,
    PublishMessageConverter messageConverter,
    ILoggerFactory loggerFactory) : IMessagePublisher
{
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }

    public Uri QueueUrl { get; internal set; }

    public SqsMessagePublisher(
        Uri queueUrl,
        IAmazonSQS client,
        PublishMessageConverter messageConverter,
        ILoggerFactory loggerFactory) : this(client, messageConverter, loggerFactory)
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
        var (messageBody, attributes, _) = messageConverter.ConvertForPublish(message, metadata, PublishDestinationType.Queue);

        var request = new SendMessageRequest
        {
            MessageBody = messageBody,
            QueueUrl = QueueUrl.AbsoluteUri
        };

        AddMessageAttributes(request.MessageAttributes, attributes);

        if (metadata?.Delay != null)
        {
            request.DelaySeconds = (int) metadata.Delay.Value.TotalSeconds;
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

    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            QueueUrl
        });
    }
}
