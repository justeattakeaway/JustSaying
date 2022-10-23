using Amazon;

namespace JustSaying.AwsTools.MessageHandling;

internal static class IamSqsPolicyBuilder{

    public static string BuildPolicyJson(SqsPolicyDetails policyDetails)
    {
        var sid = Guid.NewGuid().ToString().Replace("-", "");

        var resource = policyDetails.QueueArn;

        var topicArnWildcard = string.IsNullOrWhiteSpace(policyDetails.SourceArn)
            ? "*"
            : CreateTopicArnWildcard(policyDetails.SourceArn);

        var policyJson = $@"{{
    ""Version"" : ""2012-10-17"",
    ""Statement"" : [
        {{
            ""Sid"" : ""{sid}"",
            ""Effect"" : ""Allow"",
            ""Principal"" : {{
                ""AWS"" : ""*""
            }},
            ""Action""    : ""sqs:SendMessage"",
            ""Resource""  : ""{resource}"",
            ""Condition"" : {{
                ""ArnLike"" : {{
                    ""aws:SourceArn"" : ""{topicArnWildcard}""
                }}
            }}
        }}
    ]
}}";
        return policyJson;
    }

    private static string CreateTopicArnWildcard(string topicArn)
    {
        var arn = Arn.Parse(topicArn);
        arn.Resource = "*";
        return arn.ToString();
    }
}
