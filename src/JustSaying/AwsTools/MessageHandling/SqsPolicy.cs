using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SqsPolicy
{
    internal static async Task SaveAsync(SqsPolicyDetails policyDetails, IAmazonSQS client)
    {
        var topicArnWildcard = CreateTopicArnWildcard(policyDetails.SourceArn);

        var policyJson = $@"{{
    ""Version"" : ""2012-10-17"",
    ""Statement"" : [
        {{
            ""Sid"" : ""{Guid.NewGuid().ToString().Replace("-", "")}"",
            ""Effect"" : ""Allow"",
            ""Principal"" : {{
                ""AWS"" : ""*""
            }},
            ""Action""    : ""sqs:SendMessage"",
            ""Resource""  : ""{policyDetails.QueueArn}"",
            ""Condition"" : {{
                ""ArnLike"" : {{
                    ""aws:SourceArn"" : ""{topicArnWildcard}""
                }}
            }}
        }}
    ]
}}";

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

    private static string CreateTopicArnWildcard(string topicArn)
    {
        if (string.IsNullOrWhiteSpace(topicArn))
        {
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
