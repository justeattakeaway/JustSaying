using System;
using System.Threading;

namespace JustSaying.IntegrationTests.FluentMessageMuleTests
{
    public class Future<TMessage>
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

        public void Complete(TMessage message)
        {
            Value = message;
            if (_action != null)
            {
                _action();
            }
            _event.Set();
            IsCompleted = true;
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