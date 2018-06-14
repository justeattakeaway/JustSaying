using System;
using JustSaying.Messaging.Monitoring;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class CustomMonitor : IMessageMonitor, IMeasureHandlerExecutionTime
    {
        public void HandleException(Type messageType) { }
        public void HandleTime(long handleTimeMs) { }
        public void IssuePublishingMessage() { }
        public void IncrementThrottlingStatistic() { }
        public void HandleThrottlingTime(long handleTimeMs) { }
        public void PublishMessageTime(long handleTimeMs) { }
        public void ReceiveMessageTime(long handleTimeMs, string queueName, string region) { }
        public void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan executionTime) { }
    }
}
