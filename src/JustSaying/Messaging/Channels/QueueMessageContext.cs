using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.Messaging.Channels
{
    public class QueueMessageContext
    {
        public QueueMessageContext(Message message, SqsQueueBase queue)
        {
            Message = message;
            Queue = queue;
        }

        public Message Message { get; }
        public SqsQueueBase Queue { get; }
    }
}
