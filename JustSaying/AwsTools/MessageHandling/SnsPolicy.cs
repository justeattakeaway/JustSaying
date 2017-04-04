using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.AwsTools.MessageHandling
{
    public class SnsPolicy
    {
        private readonly IReadOnlyCollection<string> _accountIds;

        public SnsPolicy(IReadOnlyCollection<string> accountIds)
        {
            _accountIds = accountIds;
        }

        public void Save(string sourceArn, IAmazonSimpleNotificationService client)
        {
            SaveAsync(sourceArn, client)
                .GetAwaiter().GetResult();
        }

        private async Task SaveAsync(string sourceArn, IAmazonSimpleNotificationService client)
        {
            ActionIdentifier[] actions = { SNSActionIdentifiers.Subscribe };

            var snsPolicy = new Policy()
                .WithStatements(GetDefaultStatement(sourceArn))
                .WithStatements(new Statement(Statement.StatementEffect.Allow)
                    .WithPrincipals(_accountIds.Select(a => new Principal(a)).ToArray())
                    .WithResources(new Resource(sourceArn))
                    .WithActionIdentifiers(actions));
            var attributeValue = snsPolicy.ToJson();
            var setQueueAttributesRequest = new SetTopicAttributesRequest(sourceArn, "Policy", attributeValue);

            await client.SetTopicAttributesAsync(setQueueAttributesRequest);
        }

        private static Statement GetDefaultStatement(string sourceArn)
        {
            var sourceAccountId = ExtractSourceAccountId(sourceArn);
            return new Statement(Statement.StatementEffect.Allow)
                .WithPrincipals(Principal.AllUsers)
                .WithActionIdentifiers(
                    SNSActionIdentifiers.GetTopicAttributes,
                    SNSActionIdentifiers.SetTopicAttributes,
                    SNSActionIdentifiers.AddPermission,
                    SNSActionIdentifiers.RemovePermission,
                    SNSActionIdentifiers.DeleteTopic,
                    SNSActionIdentifiers.Subscribe,
                    SNSActionIdentifiers.ListSubscriptionsByTopic,
                    SNSActionIdentifiers.Publish)
                .WithResources(new Resource(sourceArn))
                .WithConditions(new Condition("StringEquals", "AWS:SourceOwner", sourceAccountId));
        }

        private static string ExtractSourceAccountId(string sourceArn)
        {
            //Sns Arn pattern: arn:aws:sns:region:account-id:topic
            var match = Regex.Match(sourceArn, "(.*?):(.*?):(.*?):(.*?):(.*?):(.*?)", RegexOptions.None, Regex.InfiniteMatchTimeout);
            return match.Groups[5].Value;
        }
    }
}
