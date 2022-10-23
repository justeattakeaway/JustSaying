using System.Text.Json;
using System.Text.RegularExpressions;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicyBuilder
{
    internal static string BuildPolicyJson(SnsPolicyDetails policyDetails)
    {
        var sourceAccountId = ExtractSourceAccountId(policyDetails.SourceArn);
        var policyJson = $@"{{
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
                    ""AWS:SourceOwner"" : ""{sourceAccountId}""
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
        return policyJson;
    }

    private static string ExtractSourceAccountId(string sourceArn)
    {
        //Sns Arn pattern: arn:aws:sns:region:account-id:topic
        var match = Regex.Match(sourceArn, "(.*?):(.*?):(.*?):(.*?):(.*?):(.*?)", RegexOptions.None, Regex.InfiniteMatchTimeout);
        return match.Groups[5].Value;
    }
}
