using System.Linq;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsQueueByUrl : SqsQueueBase
    {
        public SqsQueueByUrl(RegionEndpoint region, string queueUrl, IAmazonSQS client)
            : base(region, client)
        {
            Url = queueUrl;
        }

        public override bool Exists()
        {
            // todo make async
            var result = Client.ListQueuesAsync(new ListQueuesRequest())
                .GetAwaiter().GetResult();

            if (result.QueueUrls.Any(x => x == Url))
            {
                SetQueueProperties();
                // Need to set the prefix yet!
                return true;
            }

            return false;
        }
    }
}