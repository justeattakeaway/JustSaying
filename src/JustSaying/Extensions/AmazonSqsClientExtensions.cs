using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.Extensions
{
    internal static class AmazonSqsClientExtensions
    {
        public static async Task TagQueueAsync(this IAmazonSQS client, string queueUrl, Dictionary<string, string> tags, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            await client.TagQueueAsync(new TagQueueRequest()
                {
                    QueueUrl = queueUrl,
                    Tags = tags
                },
                cancellationToken);
        }

        public static async Task<IList<Message>> ReceiveMessagesAsync(this IAmazonSQS client, string queueUrl, int maxNumOfMessages, int secondsWaitTime, IList<string> attributesToLoad, CancellationToken cancellationToken)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            var result = await client.ReceiveMessageAsync(new ReceiveMessageRequest(queueUrl)
                {
                    AttributeNames = attributesToLoad.ToList(),
                    WaitTimeSeconds = secondsWaitTime,
                    MaxNumberOfMessages = maxNumOfMessages
                },
                cancellationToken).ConfigureAwait(false);

            if (result?.Messages != null) return result.Messages;
            return Array.Empty<Message>();
        }
    }
}
