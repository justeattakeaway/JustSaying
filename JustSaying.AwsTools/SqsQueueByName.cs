using System.Collections.Generic;
using System.Globalization;
using Amazon.SQS;
using Amazon.SQS.Model;
using Amazon.SQS.Util;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.AwsTools
{
    public class SqsQueueByName : SqsQueueByNameBase
    {
        private readonly int _retryCountBeforeSendingToErrorQueue;

        public SqsQueueByName(string queueName, IAmazonSQS client, int retryCountBeforeSendingToErrorQueue)
            : base(queueName, client)
        {
            _retryCountBeforeSendingToErrorQueue = retryCountBeforeSendingToErrorQueue;
            ErrorQueue = new ErrorQueue(queueName, client);
        }

        protected override Dictionary<string, string> GetCreateQueueAttributes(int retentionPeriodSeconds, int visibilityTimeoutSeconds)
        {
            return new Dictionary<string, string>
            {
                { SQSConstants.ATTRIBUTE_MESSAGE_RETENTION_PERIOD , retentionPeriodSeconds.ToString(CultureInfo.InvariantCulture)},
                { SQSConstants.ATTRIBUTE_VISIBILITY_TIMEOUT  , visibilityTimeoutSeconds.ToString(CultureInfo.InvariantCulture)},
                { JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, new RedrivePolicy(_retryCountBeforeSendingToErrorQueue, ErrorQueue.Arn).ToString()}
            };
        }

        // ToDO: int attempt because it's recursive. Let's clean that up for peeps.
        public override bool Create(int retentionPeriodSeconds, int attempt = JustSayingConstants.DEFAULT_CREATE_REATTEMPT, int visibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT, bool createErrorQueue = false, int retryCountBeforeSendingToErrorQueue = JustSayingConstants.DEFAULT_HANDLER_RETRY_COUNT)
        {
            if (!ErrorQueue.Exists())
            {
                ErrorQueue.Create(JustSayingConstants.MAXIMUM_RETENTION_PERIOD, JustSayingConstants.DEFAULT_CREATE_REATTEMPT, JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT, errorQueueOptOut: true);
            }
            return base.Create(retentionPeriodSeconds, attempt, visibilityTimeoutSeconds);
        }

        public override void Delete()
        {
            if(ErrorQueue != null)
                ErrorQueue.Delete();
            base.Delete();
        }

        public void UpdateRedrivePolicy(RedrivePolicy requestedRedrivePolicy)
        {
            if (RedrivePolicy.MaximumReceives != requestedRedrivePolicy.MaximumReceives)
            {
                Client.SetQueueAttributes(
                new SetQueueAttributesRequest
                {
                    QueueUrl = Url,
                    Attributes = new Dictionary<string, string> { { JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, requestedRedrivePolicy.ToString() } }
                });
            }
        }
    }
}