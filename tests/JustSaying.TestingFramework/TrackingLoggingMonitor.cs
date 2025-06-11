using System.Collections.Concurrent;
using Amazon.SQS.Model;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework;

public class TrackingLoggingMonitor(ILogger<TrackingLoggingMonitor> logger) : IMessageMonitor
{
    private readonly ConcurrentBag<(Type handlerType, Type messageType, TimeSpan duration)> _handlerExecutionTimes = [];
    private readonly ConcurrentBag<Type> _handledExceptions = [];
    private readonly ConcurrentBag<(Exception exception, Message message)> _handledErrors = [];
    private readonly ConcurrentBag<TimeSpan> _handledTimes = [];
    private readonly ConcurrentBag<TimeSpan> _handledThrottlingTime = [];
    private readonly ConcurrentBag<TimeSpan> _publishMessageTimes = [];
    private readonly ConcurrentBag<Models.Message> _handledMessages = [];
    private readonly ConcurrentBag<(TimeSpan duration, string queue, string region)> _receiveMessageTimes = [];

    public IReadOnlyCollection<(Type handlerType, Type messageType, TimeSpan duration)> HandlerExecutionTimes => _handlerExecutionTimes;
    public IReadOnlyCollection<Type> HandledExceptions => _handledExceptions;
    public IReadOnlyCollection<(Exception exception, Message message)> HandledErrors => _handledErrors;
    public IReadOnlyCollection<TimeSpan> HandledTimes => _handledTimes;
    public IReadOnlyCollection<TimeSpan> HandledThrottlingTime => _handledThrottlingTime;
    public IReadOnlyCollection<TimeSpan> PublishMessageTimes => _publishMessageTimes;
    public IReadOnlyCollection<Models.Message> HandledMessages => _handledMessages;
    public IReadOnlyCollection<(TimeSpan duration, string queue, string region)> ReceiveMessageTimes => _receiveMessageTimes;

    public int IssuesPublishingMessage { get; private set; }
    public int ThrottlingStatisticIncrements { get; private set; }

    public void HandleException(Type messageType)
    {
        _handledExceptions.Add(messageType);
        logger.LogInformation("Exception occurred when handling message of type {MessageType}", messageType.FullName);
    }

    public void HandleError(Exception ex, Message message)
    {
        _handledErrors.Add((ex, message));
        logger.LogInformation("Handled Error for message type {MessageType}", message.GetType().FullName);
    }

    public void HandleTime(TimeSpan duration)
    {
        _handledTimes.Add(duration);
        logger.LogInformation("Message handled in {Duration}", duration);
    }

    public void IssuePublishingMessage()
    {
        IssuesPublishingMessage++;
        logger.LogInformation("Problem during publish");
    }

    public void Handled(Models.Message message)
    {
        _handledMessages.Add(message);
        logger.LogInformation("Handled message of type {MessageType}", message.GetType());
    }

    public void IncrementThrottlingStatistic()
    {
        ThrottlingStatisticIncrements++;
    }

    public void HandleThrottlingTime(TimeSpan duration)
    {
        _handledThrottlingTime.Add(duration);
        logger.LogInformation("MessageReceiveBuffer throttled for {Duration}", duration);
    }

    public void PublishMessageTime(TimeSpan duration)
    {
        _publishMessageTimes.Add(duration);
        logger.LogInformation("Message was published in {Duration}", duration);
    }

    public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
    {
        _receiveMessageTimes.Add((duration, queueName, region));
        logger.LogInformation(
            "MessageReceiveBuffer spent {Duration} receiving messages from {QueueName} in region {Region}",
            duration,
            queueName,
            region);
    }

    public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration)
    {
        _handlerExecutionTimes.Add((handlerType, messageType, duration));
        logger.LogInformation("Handler type {HandlerType} spent {Duration} handling message of type {MessageType}",
            handlerType,
            duration,
            messageType);
    }
}
