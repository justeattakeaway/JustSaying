using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SqsPolicy
{
    internal static async Task SaveAsync(SqsPolicyDetails policyDetails, IAmazonSQS client)
    {
        var policyJson = SqsPolicyBuilder.BuildPolicyJson(policyDetails);

        var setQueueAttributesRequest = new SetQueueAttributesRequest
        {
            QueueUrl = policyDetails.QueueUri.AbsoluteUri,
            Attributes =
            {
                ["Policy"] = policyJson
            }
        };

        await client.SetQueueAttributesAsync(setQueueAttributesRequest).ConfigureAwait(false);
    }
}
