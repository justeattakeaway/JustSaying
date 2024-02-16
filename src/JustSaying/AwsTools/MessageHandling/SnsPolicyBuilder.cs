using System.Text.Json;
using Amazon;
using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicyBuilder
{
    internal static string BuildPolicyJson(SnsPolicyDetails policyDetails)
    {
        if (!Arn.IsArn(policyDetails.SourceArn) || !Arn.TryParse(policyDetails.SourceArn, out var arn))
        {
            throw new ArgumentException("Must be a valid ARN.", nameof(policyDetails));
        }
        var accountId = arn.AccountId;
        return $@"{{
    ""Version"" : ""2012-10-17"",
    ""Statement"" : [
        {{
            ""Sid"" : ""{Guid.NewGuid().ToString().Replace("-", "")}"",
            ""Effect"" : ""Allow"",
            ""Principal"" : {{
                ""AWS"" : ""*""
            }},
            ""Action""    : [
                ""sns:GetTopicAttributes"",
                ""sns:SetTopicAttributes"",
                ""sns:AddPermission"",
                ""sns:RemovePermission"",
                ""sns:DeleteTopic"",
                ""sns:Subscribe"",
                ""sns:Publish""
            ],
            ""Resource""  : ""{policyDetails.SourceArn}"",
            ""Condition"" : {{
                ""StringEquals"" : {{
                    ""AWS:SourceOwner"" : ""{accountId}""
                }}
            }}
        }},
        {{
            ""Sid"" : ""{Guid.NewGuid().ToString().Replace("-", "")}"",
            ""Effect"" : ""Allow"",
            ""Principal"" : {{
                ""AWS"" : {SerializeAccountIds(policyDetails.AccountIds)}
            }},
            ""Action""    : ""sns:Subscribe"",
            ""Resource""  : ""{policyDetails.SourceArn}""
        }}
    ]
}}";
    }

    private static string SerializeAccountIds(IReadOnlyCollection<string> accountIds)
    {
#if NET8_0_OR_GREATER
        return JsonSerializer.Serialize(accountIds, JustSayingSerializationContext.Default.IReadOnlyCollectionString);
#else
        return JsonSerializer.Serialize(accountIds);
#endif
    }
}
