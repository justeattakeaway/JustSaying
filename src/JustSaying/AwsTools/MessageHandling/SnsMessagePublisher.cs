using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SimpleNotificationService.Model.MessageAttributeValue;

namespace JustSaying.AwsTools.MessageHandling;

internal sealed class SnsMessagePublisher(
    IAmazonSimpleNotificationService client,
    IPublishMessageConverter messageConverter,
    ILoggerFactory loggerFactory,
    Func<Exception, Message, bool> handleException = null) : IMessagePublisher, IInterrogable
{
    private readonly IPublishMessageConverter _messageConverter = messageConverter;
    private readonly Func<Exception, Message, bool> _handleException = handleException;
    private readonly IAmazonSimpleNotificationService _client = client;
    public Action<MessageResponse, Message> MessageResponseLogger { get; set; }
    public string Arn { get; internal set; }
    private readonly ILogger _logger = loggerFactory.CreateLogger("JustSaying.Publish");

    public SnsMessagePublisher(
        string topicArn,
        IAmazonSimpleNotificationService client,
        IPublishMessageConverter messageConverter,
        ILoggerFactory loggerFactory,
        Func<Exception, Message, bool> handleException = null)
        : this(client, messageConverter, loggerFactory,handleException)
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
        var request = await BuildPublishRequestAsync(message, metadata);
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

    private async Task<PublishRequest> BuildPublishRequestAsync(Message message, PublishMetadata metadata)
    {
        var (messageToSend, attributes, subject) = await _messageConverter.ConvertForPublishAsync(message, metadata);

        var request = new PublishRequest
        {
            TopicArn = Arn,
            Subject = subject,
            Message = messageToSend,
        };

        AddMessageAttributes(request.MessageAttributes, attributes);

        return request;
    }

    private static void AddMessageAttributes(Dictionary<string, MessageAttributeValue> requestMessageAttributes, Dictionary<string, Messaging.MessageAttributeValue> attributes)
    {
        if (attributes == null || attributes.Count == 0)
        {
            return;
        }

        foreach (var attribute in attributes)
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
            Arn
        });
    }
}
