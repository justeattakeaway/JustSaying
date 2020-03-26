using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class CustomMonitor : IMessageMonitor, IMeasureHandlerExecutionTime
    {
        public void HandleException(Type messageType) { }
        public void HandleTime(TimeSpan duration) { }
        public void IssuePublishingMessage() { }
        public void IncrementThrottlingStatistic() { }
        public void HandleThrottlingTime(TimeSpan duration) { }
        public void PublishMessageTime(TimeSpan duration) { }
        public void ReceiveMessageTime(TimeSpan duration, string queueName, string region) { }
        public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration) { }
    }
}
