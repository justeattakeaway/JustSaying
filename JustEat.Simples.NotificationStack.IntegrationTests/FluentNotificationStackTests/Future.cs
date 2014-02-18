using System;
using System.Threading;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public class Future<TMessage> : IHandler<TMessage>
    {
        private readonly Action _action;
        private readonly ManualResetEvent _event;

        public Future(): this(null)
        {
        }

        public Future(Action action)
        {
            _event = new ManualResetEvent(false);
            _action = action;
        }

        public bool Handle(TMessage message)
        {
            Value = message;
            IsCompleted = true;
            _event.Set();
            if (_action != null)
            {
                _action();
            }
            return true;
        }

        public Exception RecordedException { get; set; }

        public TMessage Value { get; set; }
        public bool IsCompleted { get; private set; }

        public bool WaitUntilCompletion(TimeSpan seconds)
        {
            var completed =  _event.WaitOne(seconds);
            return completed;
        }
    }
}