using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class RecordingMessageContextAccessor : IMessageContextAccessor, IMessageContextReader
    {
        private readonly IMessageContextAccessor _inner;
        private readonly List<MessageContext> _valuesWritten = new List<MessageContext>();

        public IReadOnlyCollection<MessageContext> ValuesWritten => _valuesWritten;

        public RecordingMessageContextAccessor(IMessageContextAccessor inner)
        {
            _inner = inner;
        }

        public MessageContext MessageContext
        {
            get => _inner.MessageContext;
            set
            {
                if (value != null)
                {
                    _valuesWritten.Add(value);
                }

                _inner.MessageContext = value;
            }
        }
    }
}
