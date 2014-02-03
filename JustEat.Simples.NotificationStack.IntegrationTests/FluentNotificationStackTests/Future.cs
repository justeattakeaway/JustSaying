using System;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class Future<TMessage> : IHandler<TMessage>
    {
        private readonly ManualResetEvent _event;

        public Future()
        {
            _event = new ManualResetEvent(false);
        }
        public bool Handle(TMessage message)
        {
            Value = message;
            IsCompleted = true;
            _event.Set();
            return true;
        }

        public TMessage Value { get; set; }
        public bool IsCompleted { get; private set; }

        public bool WaitUntilCompletion(TimeSpan seconds)
        {
            return _event.WaitOne(seconds);
        }
    }
}