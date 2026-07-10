using JustSaying.Messaging.MessageSerialization;

namespace JustSaying.UnitTests.Messaging.MessageSerialization;

public class WhenUsingTheMessageTypeRegistry
{
    private sealed class OrderPlaced;

    [Test]
    public void GetLogicalNameUsesTheSubjectProviderAndPopulatesTheReverseLookup()
    {
        var registry = new MessageTypeRegistry(new NonGenericMessageSubjectProvider());

        var name = registry.GetLogicalName(typeof(OrderPlaced));

        name.ShouldBe(nameof(OrderPlaced));
        registry.TryResolveType(nameof(OrderPlaced), out var resolved).ShouldBeTrue();
        resolved.ShouldBe(typeof(OrderPlaced));
    }

    [Test]
    public void RegisterOverridesTheLogicalNameAndResolvesBothWays()
    {
        var registry = new MessageTypeRegistry(new NonGenericMessageSubjectProvider());

        registry.Register(typeof(OrderPlaced), "com.justeattakeaway.orders.orderplaced");

        registry.GetLogicalName(typeof(OrderPlaced)).ShouldBe("com.justeattakeaway.orders.orderplaced");
        registry.TryResolveType("com.justeattakeaway.orders.orderplaced", out var resolved).ShouldBeTrue();
        resolved.ShouldBe(typeof(OrderPlaced));
    }

    [Test]
    public void TryResolveTypeReturnsFalseForAnUnknownName()
    {
        var registry = new MessageTypeRegistry(new NonGenericMessageSubjectProvider());

        registry.TryResolveType("unknown", out var resolved).ShouldBeFalse();
        resolved.ShouldBeNull();
    }

    [Test]
    public void TheConfigExposesADefaultRegistryBackedByItsSubjectProvider()
    {
        var config = new MessagingConfig();

        config.MessageTypeRegistry.ShouldNotBeNull();
        config.MessageTypeRegistry.GetLogicalName(typeof(OrderPlaced)).ShouldBe(nameof(OrderPlaced));
    }
}
