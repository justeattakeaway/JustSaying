using System;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.TestingFramework
{
    public class LoggingMonitor : IMessageMonitor
    {
        private ILogger _logger;

        public LoggingMonitor(ILogger logger)
        {
            _logger = logger;
        }

        public void HandleException(Type messageType)
        {
            _logger.LogInformation("Handled Exception of type {ExceptionType}", messageType.FullName);
        }

        public void HandleError(Exception ex, Message message)
        {
            _logger.LogInformation("Handled Error for message type {MessageType}", message.GetType().FullName);
        }

        public void HandleTime(TimeSpan duration)
        {
            _logger.LogInformation("Message handled in {Duration}", duration);
        }

        public void IssuePublishingMessage()
        {
            _logger.LogInformation("Problem during publish");
        }

        public void IncrementThrottlingStatistic()
        {
        }

        public void HandleThrottlingTime(TimeSpan duration)
        {
            _logger.LogInformation("MessageReceiveBuffer throttled for {Duration}", duration);
        }

        public void PublishMessageTime(TimeSpan duration)
        {
            _logger.LogInformation("Message was published in {Duration}", duration);
        }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
        {
            _logger.LogInformation(
                "MessageReceiveBuffer spent {Duration} receiving messages from {QueueName} in region {Region}",
                duration,
                queueName,
                region);
        }
    }
}
