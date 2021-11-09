using System.Text.Json;
using System.Text.RegularExpressions;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.AwsTools.MessageHandling;

internal static class SnsPolicy
{
    internal static async Task SaveAsync(SnsPolicyDetails policyDetails, IAmazonSimpleNotificationService client)
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
        var setQueueAttributesRequest = new SetTopicAttributesRequest(policyDetails.SourceArn, "Policy", policyJson);

        await client.SetTopicAttributesAsync(setQueueAttributesRequest).ConfigureAwait(false);
    }

    private static string ExtractSourceAccountId(string sourceArn)
    {
        //Sns Arn pattern: arn:aws:sns:region:account-id:topic
        var match = Regex.Match(sourceArn, "(.*?):(.*?):(.*?):(.*?):(.*?):(.*?)", RegexOptions.None, Regex.InfiniteMatchTimeout);
        return match.Groups[5].Value;
    }
}