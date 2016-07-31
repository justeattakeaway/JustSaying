using System.Collections.Generic;
using System.Linq;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsPolicy
    {
        private readonly IReadOnlyCollection<string> _accountIds;

        public SnsPolicy(IReadOnlyCollection<string> accountIds)
        {
            _accountIds = accountIds;
        }

        public void Save(string sourceArn, string queueArn, string queueUrl, IAmazonSQS client)
        {
            ActionIdentifier[] actions = { SNSActionIdentifiers.Subscribe};

            var snsPolicy = new Policy()
                .WithStatements(new Statement(Statement.StatementEffect.Allow)
                    .WithPrincipals(_accountIds.Select(a => new Principal(a)).ToArray())
                    .WithResources(new Resource(queueArn))
                    .WithConditions(ConditionFactory.NewSourceArnCondition(sourceArn))
                    .WithActionIdentifiers(actions));
            var setQueueAttributesRequest = new SetTopicAttributesRequest
            {
                TopicArn = sourceArn,
                AttributeName = "Policy",
                AttributeValue = snsPolicy.ToJson()
            };
            // TODO what we gonna do with this?
        }
    }
}
