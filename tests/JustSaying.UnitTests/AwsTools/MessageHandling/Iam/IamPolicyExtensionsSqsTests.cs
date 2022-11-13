using JustSaying.AwsTools.MessageHandling;
using Newtonsoft.Json.Linq;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Iam;

public class IamPolicyExtensionsSqsTests
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
        var policy = sqsPolicyDetails.BuildIamPolicyJson();

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
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
        var policy = sqsPolicyDetails.BuildIamPolicyJson();

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
            c.WithScrubber(ScrubSids);
        });
    }

    private static string ScrubSids(string iamPolicy)
    {
        // Sids are generated from guids on each invocation so must be ignored
        // when performing approval tests
        var json = JObject.Parse(iamPolicy);
        return iamPolicy
            .Replace(json["Statement"]![0]!["Sid"]!.ToString(), "<sid>");
    }
}
