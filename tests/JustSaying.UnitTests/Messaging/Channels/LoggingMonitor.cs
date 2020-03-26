using System;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels
{
    public class LoggingMonitor : IMessageMonitor
    {
        private readonly ILogger _logger;

        public LoggingMonitor(ILogger logger)
        {
            _logger = logger;
        }

        public void HandleException(Type messageType)
        {
            _logger.LogInformation($"HandleException {messageType}.");
        }

        public void HandleThrottlingTime(TimeSpan duration)
        {
            _logger.LogInformation($"HandleThrottlingTime {duration}.");
        }

        public void HandleTime(TimeSpan duration)
        {
            _logger.LogInformation($"HandleTime {duration}.");
        }

        public void IncrementThrottlingStatistic()
        {
            _logger.LogInformation($"IncrementThrottlingStatistic.");
        }

        public void IssuePublishingMessage()
        {
            _logger.LogInformation($"IssuePublishingMessage.");
        }

        public void PublishMessageTime(TimeSpan duration)
        {
            _logger.LogInformation($"PublishMessageTime {duration}.");
        }

        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region)
        {
            _logger.LogInformation($"ReceiveMessageTime {duration}, {queueName}, {region}.");
        }
    }
}
