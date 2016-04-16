using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Models;
using JustSaying.TestingFramework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class Future<TMessage> where TMessage : Message
    {
        private readonly TaskCompletionSource<object> _doneSignal = new TaskCompletionSource<object>();

        private readonly Action _action;
        private readonly List<TMessage> _messages = new List<TMessage>();

        public Future(): this(null)
        {
        }

        public Future(Action action)
        {
            _action = action;
            ExpectedMessageCount = 1;
        }

        public void Complete(TMessage message)
        {
            try
            {
                _messages.Add(message);

                if (_action != null)
                {
                    _action();
                }
            }
            finally
            {
                if (ReceivedMessageCount >= ExpectedMessageCount)
                {
                    Tasks.DelaySendDone(_doneSignal);
                }
            }
        }

        public Task DoneSignal
        {
            get { return _doneSignal.Task; }
        }

        public int ExpectedMessageCount { get; set; }

        public int ReceivedMessageCount
        {
            get { return _messages.Count; }
        }

        public Exception RecordedException { get; set; }

        public bool HasReceived(TMessage message)
        {
            return _messages.Any(m => m.Id == message.Id);
        }
    }
}