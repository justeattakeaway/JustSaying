using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    /// <summary>
    /// Keeps track of a batch of messages that are inflight, as messages are signalled complete they are removed.
    /// </summary>
    public sealed class MessageBatchInflightTracker
    {
        readonly ConcurrentDictionary<string, Message> _messages;

        public MessageBatchInflightTracker(IEnumerable<Message> messages)
        {
            _messages = new ConcurrentDictionary<string, Message>(
                messages.Select(m => new KeyValuePair<string, Message>(m.MessageId, m)), StringComparer.Ordinal);
        }

        public void Complete(Message message) => _messages.TryRemove(message.MessageId, out _);

        public IEnumerable<Message> InflightMessages => _messages.Values;
    }
}
