using JustSaying.AwsTools.MessageHandling;
using Newtonsoft.Json.Linq;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sqs.Policy;

public class SqsPolicyBuilderTests
{
    [Fact]
    public void ShouldGenerateApprovedIamPolicy()
    {
        // arrange
        var sqsPolicyDetails = new SqsPolicyDetails
        {
            SourceArn = "arn:aws:sqs:ap-southeast-2:123456789012:topic",
        };

        // act
        var policy = SqsPolicyBuilder.BuildPolicyJson(sqsPolicyDetails);

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
            // Sids are generated from guids on each invocation so must be ignored
            // when performing approval tests
            c.WithScrubber(ScrubSids);
        });
    }

    [Fact]
    public void ShouldGenerateApprovedIamPolicyWithWildcardFromEmptySourceArn()
    {
        // arrange
        var sqsPolicyDetails = new SqsPolicyDetails
        {
            SourceArn = "",
        };

        // act
        var policy = SqsPolicyBuilder.BuildPolicyJson(sqsPolicyDetails);

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
            // Sids are generated from guids on each invocation so must be ignored
            // when performing approval tests
            c.WithScrubber(ScrubSids);
        });
    }

    private static string ScrubSids(string iamPolicy)
    {
        var json = JObject.Parse(iamPolicy);
        return iamPolicy
            .Replace(json["Statement"]![0]!["Sid"]!.ToString(), "<sid>");
    }
}
