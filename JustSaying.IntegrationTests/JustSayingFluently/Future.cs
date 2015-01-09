using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using JustSaying.Models;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class Future<TMessage> where TMessage : Message
    {
        private readonly Action _action;
        private readonly ManualResetEvent _event;
        private readonly List<TMessage> _messages = new List<TMessage>();

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
            _messages.Add(message);
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

        public bool HasReceived(TMessage message)
        {
            return _messages.Any(m => m.Id == message.Id);
        }
    }
}