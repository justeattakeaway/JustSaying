using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.UnitTests.AwsTools.QueueCreation;

public class WhenSerializingRedrivePolicy
{
    [Test]
    public void CanDeserializeIntoRedrivePolicy()
    {
        var policy = new RedrivePolicy(1, "queue");
        var policySerialized = policy.ToString();

        var outputPolicy = RedrivePolicy.ConvertFromString(policySerialized);

        outputPolicy.MaximumReceives.ShouldBe(policy.MaximumReceives);
        outputPolicy.DeadLetterQueue.ShouldBe(policy.DeadLetterQueue);
    }
}