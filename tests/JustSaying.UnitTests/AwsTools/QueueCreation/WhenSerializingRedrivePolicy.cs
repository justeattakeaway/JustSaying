using JustSaying.AwsTools.QueueCreation;
#pragma warning disable CS0618 // Type or member is obsolete

namespace JustSaying.UnitTests.AwsTools.QueueCreation;

public class WhenSerializingRedrivePolicy
{
    [Fact]
    public void CanDeserializeIntoRedrivePolicy()
    {
        var policy = new RedrivePolicy(1, "queue");
        var policySerialized = policy.ToString();

        var outputPolicy = RedrivePolicy.ConvertFromString(policySerialized);

        outputPolicy.MaximumReceives.ShouldBe(policy.MaximumReceives);
        outputPolicy.DeadLetterQueue.ShouldBe(policy.DeadLetterQueue);
    }
}
