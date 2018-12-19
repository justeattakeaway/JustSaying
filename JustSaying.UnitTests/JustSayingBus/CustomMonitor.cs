using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class CustomMonitor : IMessageMonitor, IMeasureHandlerExecutionTime
    {
        public void HandleException(Type messageType) { }
        public void HandleTime(TimeSpan handleTime) { }
        public void IssuePublishingMessage() { }
        public void IncrementThrottlingStatistic() { }
        public void HandleThrottlingTime(TimeSpan handleTime) { }
        public void PublishMessageTime(TimeSpan handleTime) { }
        public void ReceiveMessageTime(TimeSpan handleTime, string queueName, string region) { }
        public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan executionTime) { }
    }
}
