using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsQueueByUrl : SqsQueueBase
    {
        public SqsQueueByUrl(RegionEndpoint region, Uri queueUri, IAmazonSQS client, ILoggerFactory loggerFactory)
            : base(region, client, loggerFactory)
        {
            Uri = queueUri;
        }

        public override async Task<bool> ExistsAsync()
        {
            var result = await Client.ListQueuesAsync(new ListQueuesRequest()).ConfigureAwait(false);

            if (result.QueueUrls.Any(x => x == Uri.AbsoluteUri))
            {
                await SetQueuePropertiesAsync().ConfigureAwait(false);
                // Need to set the prefix yet!
                return true;
            }

            return false;
        }
    }
}
