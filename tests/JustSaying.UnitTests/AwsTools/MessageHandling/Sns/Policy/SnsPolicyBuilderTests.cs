using JustSaying.AwsTools.MessageHandling;
using Newtonsoft.Json.Linq;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.Policy;

public class SnsPolicyBuilderTests
{
    [Fact]
    public void WhenGeneratingPolicy_ForSnsTopic_ThenTheApprovedJsonShouldBeReturned()
    {
        // arrange
        var sourceArn = "arn:aws:sns:ap-southeast-2:123456789012:topic";
        var snsPolicyDetails = new SnsPolicyDetails
        {
            SourceArn = sourceArn
        };

        // act
        var policy = SnsPolicyBuilder.BuildPolicyJson(snsPolicyDetails);

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
            c.WithScrubber(ScrubSids);
        });
    }

    private static string ScrubSids(string iamPolicy)
    {
        var json = JObject.Parse(iamPolicy);
        return iamPolicy
            .Replace(json["Statement"]![0]!["Sid"]!.ToString(), "<sid1>")
            .Replace(json["Statement"]![1]!["Sid"]!.ToString(), "<sid2>");
    }
}
