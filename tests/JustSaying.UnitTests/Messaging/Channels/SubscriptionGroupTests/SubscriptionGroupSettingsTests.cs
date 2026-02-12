using JustSaying.Messaging.Channels.SubscriptionGroups;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class SubscriptionGroupSettingsTests
{
    [Fact]
    public void DefaultConcurrencyLimitType_IsInFlightMessages()
    {
        var builder = new SubscriptionGroupSettingsBuilder();

        Assert.Equal(ConcurrencyLimitType.InFlightMessages, builder.ConcurrencyLimitType);
    }

    [Fact]
    public void WithDefaultConcurrencyLimit_SingleArg_SetsTypeToInFlightMessages()
    {
        var builder = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(10);

        Assert.Equal(10, builder.ConcurrencyLimit);
        Assert.Equal(ConcurrencyLimitType.InFlightMessages, builder.ConcurrencyLimitType);
    }

    [Fact]
    public void WithDefaultConcurrencyLimit_MessagesPerSecond_SetsBothValues()
    {
        var builder = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(100, ConcurrencyLimitType.MessagesPerSecond);

        Assert.Equal(100, builder.ConcurrencyLimit);
        Assert.Equal(ConcurrencyLimitType.MessagesPerSecond, builder.ConcurrencyLimitType);
    }

    [Fact]
    public void PerGroupOverride_TakesPrecedenceOverDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.InFlightMessages);

        var configBuilder = new SubscriptionGroupConfigBuilder("test")
            .WithConcurrencyLimit(200, ConcurrencyLimitType.MessagesPerSecond);

        var settings = configBuilder.Build(defaults);

        Assert.Equal(200, settings.ConcurrencyLimit);
        Assert.Equal(ConcurrencyLimitType.MessagesPerSecond, settings.ConcurrencyLimitType);
    }

    [Fact]
    public void PerGroupWithNoOverride_UsesDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.MessagesPerSecond);

        var configBuilder = new SubscriptionGroupConfigBuilder("test");

        var settings = configBuilder.Build(defaults);

        Assert.Equal(50, settings.ConcurrencyLimit);
        Assert.Equal(ConcurrencyLimitType.MessagesPerSecond, settings.ConcurrencyLimitType);
    }

    [Fact]
    public void PerGroupOverridesOnlyLimit_InheritsTypeFromDefault()
    {
        var defaults = new SubscriptionGroupSettingsBuilder()
            .WithDefaultConcurrencyLimit(50, ConcurrencyLimitType.MessagesPerSecond);

        // Single-arg overload sets type to InFlightMessages
        var configBuilder = new SubscriptionGroupConfigBuilder("test")
            .WithConcurrencyLimit(100);

        var settings = configBuilder.Build(defaults);

        Assert.Equal(100, settings.ConcurrencyLimit);
        Assert.Equal(ConcurrencyLimitType.InFlightMessages, settings.ConcurrencyLimitType);
    }
}
