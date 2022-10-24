using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicy
{
    internal static async Task SaveAsync(SnsPolicyDetails policyDetails, IAmazonSimpleNotificationService client)
    {
        var policyJson = policyDetails.BuildIamPolicyJson();
        var setQueueAttributesRequest = new SetTopicAttributesRequest(policyDetails.SourceArn, "Policy", policyJson);
        await client.SetTopicAttributesAsync(setQueueAttributesRequest).ConfigureAwait(false);
    }
}
