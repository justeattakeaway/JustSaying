using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustEat.AwsTools
{
    public class SqsQueueByName : SqsQueueBase
    {
        public SqsQueueByName(string queueName, AmazonSQS client)
            : base(client)
        {
            QueueNamePrefix = queueName;
            Exists();
        }

        public override bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest().WithQueueNamePrefix(QueueNamePrefix));
            if (result.IsSetListQueuesResult() && result.ListQueuesResult.IsSetQueueUrl())
            {
                Url = result.ListQueuesResult.QueueUrl.First();
                SetArn();
                return true;
            }

            return false;
        }

        public bool Create(int attempt = 0)
        {
            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest().WithQueueName(QueueNamePrefix));
                if (result.IsSetCreateQueueResult() && !string.IsNullOrWhiteSpace(result.CreateQueueResult.QueueUrl))
                {
                    Url = result.CreateQueueResult.QueueUrl;
                    SetArn();
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (attempt >= 2)
                    throw;

                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Thread.Sleep(60000);
                    Create(attempt++);
                }
            }

            return false;
        }
    }
}