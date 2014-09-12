using System.Collections.Generic;
using System.Globalization;
using System.Net;
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
        public override bool Create(SqsConfiguration queueConfig, int attempt = 0)
        {
            if (!ErrorQueue.Exists())
            {
                ErrorQueue.Create(new SqsConfiguration(){ErrorQueueRetentionPeriodSeconds = queueConfig.ErrorQueueRetentionPeriodSeconds, ErrorQueueOptOut = true});
            }
            return base.Create(queueConfig, attempt: attempt);
        }

        public override void Delete()
        {
            if(ErrorQueue != null)
                ErrorQueue.Delete();
            base.Delete();
        }

        public void UpdateRedrivePolicy(RedrivePolicy requestedRedrivePolicy)
        {
            if (RedrivePolicyNeedsUpdating(requestedRedrivePolicy))
            {
                var response = Client.SetQueueAttributes(
                new SetQueueAttributesRequest
                {
                    QueueUrl = Url,
                    Attributes = new Dictionary<string, string> { { JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY, requestedRedrivePolicy.ToString() } }
                });
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    RedrivePolicy = requestedRedrivePolicy;
                }
            }
        }

        private bool RedrivePolicyNeedsUpdating(RedrivePolicy requestedRedrivePolicy)
        {
            return RedrivePolicy == null || RedrivePolicy.MaximumReceives != requestedRedrivePolicy.MaximumReceives;
        }

        public void UpdateQueueAttribute(SqsConfiguration queueConfig)
        {
            if (QueueNeedsUpdating(queueConfig))
            {
                var response = Client.SetQueueAttributes(
                    new SetQueueAttributesRequest
                    {
                        QueueUrl = Url,
                        Attributes = new Dictionary<string, string>
                        {
                            {JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD, queueConfig.MessageRetentionSeconds.ToString()},
                            {JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT, queueConfig.VisibilityTimeoutSeconds.ToString()},
                        }
                    });
                if (response.HttpStatusCode == HttpStatusCode.OK)
                {
                    MessageRetentionPeriod = queueConfig.MessageRetentionSeconds;
                    VisibilityTimeout = queueConfig.VisibilityTimeoutSeconds;
                }
            }
        }

        private bool QueueNeedsUpdating(SqsConfiguration queueConfig)
        {
            return MessageRetentionPeriod != queueConfig.MessageRetentionSeconds
                   || VisibilityTimeout != queueConfig.VisibilityTimeoutSeconds;
        }
    }
}