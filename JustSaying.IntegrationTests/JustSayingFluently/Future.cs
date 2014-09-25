using System;
using System.Threading;

namespace JustSaying.IntegrationTests.JustSayingFluently
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
            _event.Set();

            Value = message;
            if (_action != null)
            {
                _action();
            }
        }

        public Exception RecordedException { get; set; }

        public TMessage Value { get; set; }

        public bool IsCompleted
        {
            get { return _event.WaitOne(0); }
            
        }

        public bool WaitUntilCompletion(TimeSpan seconds)
        {
            var completed =  _event.WaitOne(seconds);
            return completed;
        }
    }
}