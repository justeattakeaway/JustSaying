using System;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.StatsD;

namespace JustEat.Simples.NotificationStack.Stack.Monitoring
{
    public class StatsDMessageMonitor : IMessageMonitor
    {
        private readonly IStatsDPublisher _publisher;

        public StatsDMessageMonitor(IStatsDPublisher publisher)
        {
            _publisher = publisher;
        }

        public void Handled()
        {
            _publisher.Increment("notificationstack-message-handled");
        }

        public void HandleException()
        {
            _publisher.Increment("notificationstack-message-handle-exception");
        }

        public void HandleTime(long handleTimeMs)
        {
            _publisher.Timing(TimeSpan.FromMilliseconds(handleTimeMs), "notificationstack-sqs-message-handled");
        }
    }
}