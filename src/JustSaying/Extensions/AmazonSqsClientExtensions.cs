using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.Extensions
{
    public static class AmazonSqsClientExtensions
    {
        public static async Task TagQueueAsync(this IAmazonSQS client, string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
        {
            await client.TagQueueAsync(new TagQueueRequest()
                {
                    QueueUrl = queueUrl,
                    Tags = tags
                },
                cancellationToken);
        }

        public static async Task<IList<Message>> ReceiveMessagesAsync(this IAmazonSQS client, string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
        {
            var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl)
                {
                    AttributeNames = attributesToLoad.ToList(),
                    WaitTimeSeconds = secondsWaitTime,
                    MaxNumberOfMessages = maxNumOfMessages
                },
                cancellationToken).ConfigureAwait(false);

            return result?.Messages ?? new List<Message>();
        }
    }
}
