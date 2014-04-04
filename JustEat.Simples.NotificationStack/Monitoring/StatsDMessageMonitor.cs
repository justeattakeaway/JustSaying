using System;
using JustSaying.Messaging.Monitoring;
using JustEat.StatsD;

namespace JustSaying.Stack.Monitoring
{
    public class StatsDMessageMonitor : IMessageMonitor
    {
        private readonly IStatsDPublisher _publisher;

        public StatsDMessageMonitor(IStatsDPublisher publisher)
        {
            _publisher = publisher;
        }

        public void HandleException(string messageType)
        {
            _publisher.Increment(string.Format("notificationstack-message-handle-exception.{0}", messageType));
        }

        public void HandleTime(long handleTimeMs)
        {
            _publisher.Timing(TimeSpan.FromMilliseconds(handleTimeMs), "notificationstack-message-handled");
        }

        public void IssuePublishingMessage()
        {
            _publisher.Increment("notificationstack-message-publish-exception");
        }

        public void IncrementThrottlingStatistic()
        {
            _publisher.Increment("notificationstack-concurrency-throttled");
        }

        public void HandleThrottlingTime(long handleTimeMs)
        {
            _publisher.Timing(TimeSpan.FromMilliseconds(handleTimeMs), "notificationstack-message-throttle");
        }

        public void PublishMessageTime(long handleTimeMs)
        {
            _publisher.Timing(TimeSpan.FromMilliseconds(handleTimeMs), "notificationstack-message-publish");
        }

        public void ReceiveMessageTime(long handleTimeMs)
        {
            _publisher.Timing(TimeSpan.FromMilliseconds(handleTimeMs), "notificationstack-message-receive");
        }
    }
}