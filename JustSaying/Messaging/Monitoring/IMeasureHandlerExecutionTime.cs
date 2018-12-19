using System;

namespace JustSaying.Messaging.Monitoring
{
    public interface IMeasureHandlerExecutionTime
    {
        void HandlerExecutionTime(Type handlerType, Type messageType, TimeSpan duration);
    }
}
