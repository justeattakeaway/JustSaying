using System.Text.Json;
using Amazon;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicyDetailsIamExtensions
{
    internal static string BuildIamPolicyJson(this SnsPolicyDetails policyDetails)
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
                ""AWS"" : {JsonSerializer.Serialize(policyDetails.AccountIds)}
            }},
            ""Action""    : ""sns:Subscribe"",
            ""Resource""  : ""{policyDetails.SourceArn}""
        }}
    ]
}}";
    }
}
