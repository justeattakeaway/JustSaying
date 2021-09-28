using System;
using System.Collections.Generic;
using Amazon.SQS.Model;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework
{
    public class TrackingLoggingMonitor : IMessageMonitor
    {
        private readonly ILogger _logger;

        public TrackingLoggingMonitor(ILogger<TrackingLoggingMonitor> logger)
        {
            _logger = logger;
            HandledExceptions = new List<Type>();
            HandledErrors = new List<(Exception exception, Message message)>();
            HandledTimes = new List<TimeSpan>();
            HandledThrottlingTime = new List<TimeSpan>();
            PublishMessageTimes = new List<TimeSpan>();
            ReceiveMessageTimes = new List<(TimeSpan duration, string queue, string region)>();
            HandlerExecutionTimes = new List<(Type handlerType, Type messageType, TimeSpan duration)>();
            HandledMessages = new List<Models.Message>();
        }

        public List<(Type handlerType, Type messageType, TimeSpan duration)> HandlerExecutionTimes { get; }
        public IList<Type> HandledExceptions { get; }
        public IList<(Exception exception, Message message)> HandledErrors { get; }
        public IList<TimeSpan> HandledTimes { get; }
        public IList<TimeSpan> HandledThrottlingTime { get; }
        public IList<TimeSpan> PublishMessageTimes { get; }
        public IList<Models.Message> HandledMessages { get; }
        public IList<(TimeSpan duration, string queue, string region)> ReceiveMessageTimes { get; }
        public int IssuesPublishingMessage { get; private set; }
        public int ThrottlingStatisticIncrements { get; private set; }

        public void HandleException(Type messageType)
        {
            HandledExceptions.Add(messageType);
            _logger.LogInformation("Exception occurred when handling message of type {MessageType}", messageType.FullName);
        }

        public void HandleError(Exception ex, Message message)
        {
            HandledErrors.Add((ex, message));
            _logger.LogInformation("Handled Error for message type {MessageType}", message.GetType().FullName);
        }

        public void HandleTime(TimeSpan duration)
        {
            HandledTimes.Add(duration);
            _logger.LogInformation("Message handled in {Duration}", duration);
        }

        public void IssuePublishingMessage()
        {
            IssuesPublishingMessage++;
            _logger.LogInformation("Problem during publish");
        }

        public void Handled(Models.Message message)
        {
            HandledMessages.Add(message);
            _logger.LogInformation("Handled message of type {MessageType}", message.GetType());
        }

        public void IncrementThrottlingStatistic()
        {
            ThrottlingStatisticIncrements++;
        }

        public void HandleThrottlingTime(TimeSpan duration)
        {
            HandledThrottlingTime.Add(duration);
            _logger.LogInformation("MessageReceiveBuffer throttled for {Duration}", duration);
        }

        public void PublishMessageTime(TimeSpan duration)
        {
            PublishMessageTimes.Add(duration);
            _logger.LogInformation("Message was published in {Duration}", duration);
        }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
        {
            ReceiveMessageTimes.Add((duration, queueName, region));
            _logger.LogInformation(
                "MessageReceiveBuffer spent {Duration} receiving messages from {QueueName} in region {Region}",
                duration,
                queueName,
                region);
        }

        public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration)
        {
            HandlerExecutionTimes.Add((handlerType, messageType, duration));
            _logger.LogInformation("Handler type {HandlerType} spent {Duration} handling message of type {MessageType}",
                handlerType,
                duration,
                messageType);
        }
    }
}
