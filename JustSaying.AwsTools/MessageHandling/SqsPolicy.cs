using System;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SqsPolicy
    {
        private readonly string _policy;
        public SqsPolicy(string policy)
        {
            _policy = policy;
        }

        public static void Save(string sourceArn, string queueArn, string queueUrl, IAmazonSQS client)
        {
            SaveAsync(sourceArn, queueArn, queueUrl, client)
                .GetAwaiter().GetResult();
        }

        public static async Task SaveAsync(string sourceArn, string queueArn, string queueUrl, IAmazonSQS client)
        {
            var topicArnWildcard = CreateTopicArnWildcard(sourceArn);
            ActionIdentifier[] actions = { SQSActionIdentifiers.SendMessage };

            var sqsPolicy = new Policy()
                .WithStatements(new Statement(Statement.StatementEffect.Allow)
                    .WithPrincipals(Principal.AllUsers)
                    .WithResources(new Resource(queueArn))
                    .WithConditions(ConditionFactory.NewSourceArnCondition(topicArnWildcard))
                    .WithActionIdentifiers(actions));
            var setQueueAttributesRequest = new SetQueueAttributesRequest
            {
                QueueUrl = queueUrl,
                Attributes = { ["Policy"] = sqsPolicy.ToJson() }
            };

            await client.SetQueueAttributesAsync(setQueueAttributesRequest);
        }


        public override string ToString() => _policy;

        private static string CreateTopicArnWildcard(string topicArn)
        {
            if (string.IsNullOrWhiteSpace(topicArn))
            {
                // todo should not get here?
                return "*";
            }

            var index = topicArn.LastIndexOf(":", StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                topicArn = topicArn.Substring(0, index + 1);
            }

            return topicArn + "*";
        }
    }
}
