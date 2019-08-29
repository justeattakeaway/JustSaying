using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageBatchInflightTracker
    {
        readonly ConcurrentDictionary<string, string> _messages;

        public MessageBatchInflightTracker(IEnumerable<KeyValuePair<string, string>> messages)
        {
            _messages = new ConcurrentDictionary<string, string>(messages, StringComparer.Ordinal);
        }

        public void Complete(string messageId) => _messages.TryRemove(messageId, out _);

        public IEnumerable<(string messageId, string receiptHandle)> InflightMessages => _messages.Select(m => (m.Key, m.Value));
    }
}
