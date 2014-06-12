using System;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMeasureHandlerExecutionTime
    {
        void HandlerExecutionTime(string typeName, string eventName, TimeSpan executionTime);
    }
}