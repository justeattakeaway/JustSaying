using System.Linq;
using System.Threading.Tasks;
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

        public override async Task<bool> ExistsAsync()
        {
            var result = await Client.ListQueuesAsync(new ListQueuesRequest()).ConfigureAwait(false);

            if (result.QueueUrls.Any(x => x == Url))
            {
                await SetQueuePropertiesAsync().ConfigureAwait(false);
                // Need to set the prefix yet!
                return true;
            }

            return false;
        }
    }
}
