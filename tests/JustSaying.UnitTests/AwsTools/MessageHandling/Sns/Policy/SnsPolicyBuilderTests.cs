using JustSaying.AwsTools.MessageHandling;
using Newtonsoft.Json.Linq;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Sns.Policy;

public class SnsPolicyBuilderTests
{
    [Fact]
    public void ShouldGenerateApprovedIamPolicy()
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
            // Sids are generated from guids on each invocation so must be ignored
            // when performing approval tests
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

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("arn:aws:service:region:123456789012")] // missing topic
    public void ShouldThrowArgumentExceptionWhenUsingInvalidArn(string sourceArn)
    {
        // arrange
        var snsPolicyDetails = new SnsPolicyDetails
        {
            SourceArn = sourceArn
        };

        // Act + Assert
        Should.Throw<ArgumentException>(() => SnsPolicyBuilder.BuildPolicyJson(snsPolicyDetails));
    }
}
