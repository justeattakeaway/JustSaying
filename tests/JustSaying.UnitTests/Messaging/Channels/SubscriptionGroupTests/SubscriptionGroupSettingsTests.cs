using JustSaying.Messaging.Channels.SubscriptionGroups;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class SubscriptionGroupSettingsTests
{
    [Test]
    public void DefaultConcurrencyLimitType_IsInFlightMessages()
    {
        var builder = new SubscriptionGroupSettingsBuilder();

        builder.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.InFlightMessages);
    }

    [Test]
    public void WithDefaultConcurrencyLimit_SingleArg_SetsTypeToInFlightMessages()
    {
        var builder = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(10);

        builder.ConcurrencyLimit.ShouldBe(10);
        builder.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.InFlightMessages);
    }

    [Test]
    public void WithDefaultConcurrencyLimit_MessagesPerSecond_SetsBothValues()
    {
        var builder = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(100, ConcurrencyLimitType.MessagesPerSecond);

        builder.ConcurrencyLimit.ShouldBe(100);
        builder.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.MessagesPerSecond);
    }

    [Test]
    public void PerGroupOverride_TakesPrecedenceOverDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.InFlightMessages);

        var configBuilder = new SubscriptionGroupConfigBuilder("test")
            .WithConcurrencyLimit(200, ConcurrencyLimitType.MessagesPerSecond);

        var settings = configBuilder.Build(defaults);

        settings.ConcurrencyLimit.ShouldBe(200);
        settings.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.MessagesPerSecond);
    }

    [Test]
    public void PerGroupWithNoOverride_UsesDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.MessagesPerSecond);

        var configBuilder = new SubscriptionGroupConfigBuilder("test");

        var settings = configBuilder.Build(defaults);

        settings.ConcurrencyLimit.ShouldBe(50);
        settings.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.MessagesPerSecond);
    }

    [Test]
    public void PerGroupOverridesOnlyLimit_InheritsTypeFromDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.MessagesPerSecond);

        // Single-arg overload sets type to InFlightMessages
        var configBuilder = new SubscriptionGroupConfigBuilder("test")
            .WithConcurrencyLimit(100);

        var settings = configBuilder.Build(defaults);

        settings.ConcurrencyLimit.ShouldBe(100);
        settings.ConcurrencyLimitType.ShouldBe(ConcurrencyLimitType.InFlightMessages);
    }
}
