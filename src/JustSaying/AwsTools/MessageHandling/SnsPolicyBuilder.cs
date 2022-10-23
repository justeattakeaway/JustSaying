using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicyBuilder
{
    internal static string BuildPolicyJson(SnsPolicyDetails policyDetails)
    {
        var arn = Arn.Parse(policyDetails.SourceArn);
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
