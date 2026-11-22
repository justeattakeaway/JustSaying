using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.UnitTests.AwsTools.QueueCreation;

public class SqsWriteConfigurationTests
{
    [Test]
    public void NoMaximumMessageSize_FallsBackToTheSqsDefault()
    {
        var config = new SqsWriteConfiguration();

        config.EffectiveMaximumMessageSize.ShouldBe(JustSayingConstants.DefaultSqsMaximumMessageSize);
        Should.NotThrow(config.ValidateMaximumMessageSize);
    }

    [Test]
    public void MaximumMessageSize_IsUsedWhenSet()
    {
        var config = new SqsWriteConfiguration { MaximumMessageSize = 256 * 1024 };

        config.EffectiveMaximumMessageSize.ShouldBe(256 * 1024);
        Should.NotThrow(config.ValidateMaximumMessageSize);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1023)]
    [Arguments(1024 * 1024 + 1)]
    public void MaximumMessageSizeOutsideTheSupportedRange_IsRejected(int maximumMessageSize)
    {
        var config = new SqsWriteConfiguration { MaximumMessageSize = maximumMessageSize };

        Should.Throw<ConfigurationErrorsException>(config.ValidateMaximumMessageSize);
    }

    [Test]
    public void MaximumMessageSizeOutsideTheSupportedRange_IsRejectedByValidate()
    {
        var config = new SqsWriteConfiguration { QueueName = "queue", MaximumMessageSize = 1023 };

        Should.Throw<ConfigurationErrorsException>(config.Validate);
    }

    [Test]
    [Arguments(1024)]
    [Arguments(1024 * 1024)]
    public void MaximumMessageSizeAtTheBoundaries_IsAccepted(int maximumMessageSize)
    {
        var config = new SqsWriteConfiguration { MaximumMessageSize = maximumMessageSize };

        Should.NotThrow(config.ValidateMaximumMessageSize);
    }
}
