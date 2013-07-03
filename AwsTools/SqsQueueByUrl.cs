using System.Linq;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustEat.AwsTools
{
    public class SqsQueueByUrl : SqsQueueBase
    {
        public SqsQueueByUrl(string queueUrl, AmazonSQS client)
            : base(client)
        {
            Url = queueUrl;
        }

        public override bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest());
            if (result.IsSetListQueuesResult() && result.ListQueuesResult.IsSetQueueUrl() && result.ListQueuesResult.QueueUrl.Any(x => x == Url))
            {
                SetArn();
                // Need to set the prefix yet!
                return true;
            }

            return false;
        }
    }
}