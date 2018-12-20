using System;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageContext
    {
        public MessageContext(SQSMessage message, Uri queueUri)
        {
            Message = message ?? throw new ArgumentNullException(nameof(message));
            QueueUri = queueUri ?? throw new ArgumentNullException(nameof(queueUri));
        }

        public SQSMessage Message { get; }
        public Uri QueueUri { get; }
    }
}
