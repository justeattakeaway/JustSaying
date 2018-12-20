using System.Collections.Generic;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.IntegrationTests.WhenRegisteringHandlersViaResolver
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
            get
            {
                return _inner.MessageContext;
            }
            set
            {
                _valuesWritten.Add(value);
                _inner.MessageContext = value;
            }
        }
    }
}
