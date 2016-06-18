using System;
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
            var topicArnWildcard = CreateTopicArnWildcard(sourceArn);
            ActionIdentifier[] actions = { SQSActionIdentifiers.SendMessage };

            Policy sqsPolicy = new Policy()
                .WithStatements(new Statement(Statement.StatementEffect.Allow)
                    .WithPrincipals(Principal.AllUsers)
                    .WithResources(new Resource(queueArn))
                    .WithConditions(ConditionFactory.NewSourceArnCondition(topicArnWildcard))
                    .WithActionIdentifiers(actions));
            SetQueueAttributesRequest setQueueAttributesRequest = new SetQueueAttributesRequest();
            setQueueAttributesRequest.QueueUrl = queueUrl;
            setQueueAttributesRequest.Attributes["Policy"] = sqsPolicy.ToJson(); 
            client.SetQueueAttributes(setQueueAttributesRequest);
        }

        public override string ToString()
        {
            return _policy;
        }

        private static string CreateTopicArnWildcard(string topicArn)
        {
            int index = topicArn.LastIndexOf(":", StringComparison.InvariantCultureIgnoreCase);
            if (index > 0)
                topicArn = topicArn.Substring(0, index + 1);
            return topicArn + "*";
        }
    }
}