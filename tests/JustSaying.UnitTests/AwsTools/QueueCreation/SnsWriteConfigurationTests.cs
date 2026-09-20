using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.UnitTests.AwsTools.QueueCreation;

public class SnsWriteConfigurationTests
{
    [Test]
    public void NoMaximumMessageSize_FallsBackToTheSnsDefault()
    {
        var config = new SnsWriteConfiguration();

        config.EffectiveMaximumMessageSize.ShouldBe(JustSayingConstants.DefaultSnsMaximumMessageSize);
        Should.NotThrow(config.Validate);
    }

    [Test]
    public void MaximumMessageSize_IsUsedWhenSet()
    {
        var config = new SnsWriteConfiguration { MaximumMessageSize = JustSayingConstants.MaximumSnsMessageSize };

        config.EffectiveMaximumMessageSize.ShouldBe(JustSayingConstants.MaximumSnsMessageSize);
        Should.NotThrow(config.Validate);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1023)]
    [Arguments(1024 * 1024 + 1)]
    public void MaximumMessageSizeOutsideTheSupportedRange_IsRejected(int maximumMessageSize)
    {
        var config = new SnsWriteConfiguration { MaximumMessageSize = maximumMessageSize };

        Should.Throw<ConfigurationErrorsException>(config.Validate);
    }

    [Test]
    [Arguments(1024)]
    [Arguments(1024 * 1024)]
    public void MaximumMessageSizeAtTheBoundaries_IsAccepted(int maximumMessageSize)
    {
        var config = new SnsWriteConfiguration { MaximumMessageSize = maximumMessageSize };

        Should.NotThrow(config.Validate);
    }
}
