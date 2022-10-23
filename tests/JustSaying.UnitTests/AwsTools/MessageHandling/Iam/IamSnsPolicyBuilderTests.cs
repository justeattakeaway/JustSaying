using JustSaying.AwsTools.MessageHandling;
using Newtonsoft.Json.Linq;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.Iam;

public class IamSnsPolicyBuilderTests
{
    [Fact]
    public void ShouldGenerateApprovedIamPolicy()
    {
        // arrange
        var snsPolicyDetails = new SnsPolicyDetails
        {
            SourceArn = "arn:aws:sns:ap-southeast-2:123456789012:topic",
            AccountIds = new[] { "123456789012" }
        };

        // act
        var policy = IamSnsPolicyBuilder.BuildPolicyJson(snsPolicyDetails);

        // assert
        policy.ShouldMatchApproved(c =>
        {
            c.SubFolder("Approvals");
            c.WithScrubber(ScrubSids);
        });
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("arn:aws:service:region:123456789012")]// missing topic
    public void ShouldThrowArgumentExceptionWhenUsingInvalidArn(string sourceArn)
    {
        // arrange
        var snsPolicyDetails = new SnsPolicyDetails
        {
            SourceArn = sourceArn
        };

        // Act + Assert
        Should.Throw<ArgumentException>(() => IamSnsPolicyBuilder.BuildPolicyJson(snsPolicyDetails));
    }

    private static string ScrubSids(string iamPolicy)
    {
        // Sids are generated from guids on each invocation so must be ignored
        // when performing approval tests
        var json = JObject.Parse(iamPolicy);
        return iamPolicy
            .Replace(json["Statement"]![0]!["Sid"]!.ToString(), "<sid1>")
            .Replace(json["Statement"]![1]!["Sid"]!.ToString(), "<sid2>");
    }
}
