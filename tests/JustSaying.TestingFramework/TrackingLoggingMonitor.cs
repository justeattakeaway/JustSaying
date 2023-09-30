using Amazon.SQS.Model;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework;

public class TrackingLoggingMonitor(ILogger<TrackingLoggingMonitor> logger) : IMessageMonitor
{
    public List<(Type handlerType, Type messageType, TimeSpan duration)> HandlerExecutionTimes { get; } = [];
    public IList<Type> HandledExceptions { get; } = new List<Type>();
    public IList<(Exception exception, Message message)> HandledErrors { get; } = new List<(Exception exception, Message message)>();
    public IList<TimeSpan> HandledTimes { get; } = new List<TimeSpan>();
    public IList<TimeSpan> HandledThrottlingTime { get; } = new List<TimeSpan>();
    public IList<TimeSpan> PublishMessageTimes { get; } = new List<TimeSpan>();
    public IList<Models.Message> HandledMessages { get; } = new List<Models.Message>();
    public IList<(TimeSpan duration, string queue, string region)> ReceiveMessageTimes { get; } = new List<(TimeSpan duration, string queue, string region)>();
    public int IssuesPublishingMessage { get; private set; }
    public int ThrottlingStatisticIncrements { get; private set; }

    public void HandleException(Type messageType)
    {
        HandledExceptions.Add(messageType);
        logger.LogInformation("Exception occurred when handling message of type {MessageType}", messageType.FullName);
    }

    public void HandleError(Exception ex, Message message)
    {
        HandledErrors.Add((ex, message));
        logger.LogInformation("Handled Error for message type {MessageType}", message.GetType().FullName);
    }

    public void HandleTime(TimeSpan duration)
    {
        HandledTimes.Add(duration);
        logger.LogInformation("Message handled in {Duration}", duration);
    }

    public void IssuePublishingMessage()
    {
        IssuesPublishingMessage++;
        logger.LogInformation("Problem during publish");
    }

    public void Handled(Models.Message message)
    {
        HandledMessages.Add(message);
        logger.LogInformation("Handled message of type {MessageType}", message.GetType());
    }

    public void IncrementThrottlingStatistic()
    {
        ThrottlingStatisticIncrements++;
    }

    public void HandleThrottlingTime(TimeSpan duration)
    {
        HandledThrottlingTime.Add(duration);
        logger.LogInformation("MessageReceiveBuffer throttled for {Duration}", duration);
    }

    public void PublishMessageTime(TimeSpan duration)
    {
        PublishMessageTimes.Add(duration);
        logger.LogInformation("Message was published in {Duration}", duration);
    }

    public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
    {
        ReceiveMessageTimes.Add((duration, queueName, region));
        logger.LogInformation(
            "MessageReceiveBuffer spent {Duration} receiving messages from {QueueName} in region {Region}",
            duration,
            queueName,
            region);
    }

    public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration)
    {
        HandlerExecutionTimes.Add((handlerType, messageType, duration));
        logger.LogInformation("Handler type {HandlerType} spent {Duration} handling message of type {MessageType}",
            handlerType,
            duration,
            messageType);
    }
}
