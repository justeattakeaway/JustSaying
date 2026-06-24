using JustSaying.Fluent;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Fluent;

public class WhenUsingMessagingBusBuilder
{
    private readonly MessagingBusBuilder _sut;

    public WhenUsingMessagingBusBuilder()
    {
        _sut = new MessagingBusBuilder();
    }

    [Test]
    public void ThenPropertiesDictionaryIsInitialized()
    {
        // Assert
        _sut.Properties.ShouldNotBeNull();
        _sut.Properties.ShouldBeEmpty();
    }

    [Test]
    public void ThenPropertiesDictionaryCanStoreValues()
    {
        // Act
        _sut.Properties["key1"] = "value1";
        _sut.Properties["key2"] = 42;

        // Assert
        _sut.Properties.ShouldContainKey("key1");
        _sut.Properties.ShouldContainKey("key2");
        _sut.Properties["key1"].ShouldBe("value1");
        _sut.Properties["key2"].ShouldBe(42);
    }

    [Test]
    public void ThenMessagingConfigIsAccessible()
    {
        // Assert
        _sut.MessagingConfig.ShouldNotBeNull();
    }

    [Test]
    public void ThenMessagingConfigCanBeSet()
    {
        // Arrange
        var newConfig = new MessagingConfigurationBuilder(_sut);

        // Act
        _sut.MessagingConfig = newConfig;

        // Assert
        _sut.MessagingConfig.ShouldBe(newConfig);
    }

    [Test]
    public void ThenClientConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Client(null));
    }

    [Test]
    public void ThenClientConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Client(c => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenMessagingConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Messaging(null));
    }

    [Test]
    public void ThenMessagingConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Messaging(m => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenPublicationsConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Publications(null));
    }

    [Test]
    public void ThenPublicationsConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Publications(p => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenServicesConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Services(null));
    }

    [Test]
    public void ThenServicesConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Services(s => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenSubscriptionsConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Subscriptions(null));
    }

    [Test]
    public void ThenSubscriptionsConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Subscriptions(s => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenWithServiceResolverThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithServiceResolver(null));
    }

    [Test]
    public void ThenWithServiceResolverReturnsBuilder()
    {
        // Arrange
        var resolver = NSubstitute.Substitute.For<IServiceResolver>();

        // Act
        var result = _sut.WithServiceResolver(resolver);

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenMultipleConfigurationCallsCanBeChained()
    {
        // Act
        var result = _sut
            .Messaging(m => m.WithRegion("us-east-1"))
            .Publications(p => { })
            .Subscriptions(s => { })
            .Services(s => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Test]
    public void ThenPropertiesCanBeUsedForExtensions()
    {
        // Arrange
        var customConfig = new { ConnectionString = "localhost:9092" };

        // Act
        _sut.Properties["CustomMessagingConfig"] = customConfig;

        // Assert
        _sut.Properties["CustomMessagingConfig"].ShouldBe(customConfig);
    }

    [Test]
    public void ThenGetPropertyReturnsValueWhenExists()
    {
        // Arrange
        _sut.Properties["TestKey"] = "TestValue";

        // Act
        var result = _sut.GetProperty<string>("TestKey");

        // Assert
        result.ShouldBe("TestValue");
    }

    [Test]
    public void ThenGetPropertyReturnsDefaultWhenKeyNotFound()
    {
        // Act
        var result = _sut.GetProperty<string>("NonExistentKey");

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ThenGetPropertyReturnsDefaultWhenTypeMismatch()
    {
        // Arrange
        _sut.Properties["TestKey"] = 42;

        // Act
        var result = _sut.GetProperty<string>("TestKey");

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ThenTryGetPropertyReturnsTrueWhenExists()
    {
        // Arrange
        _sut.Properties["TestKey"] = "TestValue";

        // Act
        var success = _sut.TryGetProperty<string>("TestKey", out var value);

        // Assert
        success.ShouldBeTrue();
        value.ShouldBe("TestValue");
    }

    [Test]
    public void ThenTryGetPropertyReturnsFalseWhenKeyNotFound()
    {
        // Act
        var success = _sut.TryGetProperty<string>("NonExistentKey", out var value);

        // Assert
        success.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Test]
    public void ThenTryGetPropertyReturnsFalseWhenTypeMismatch()
    {
        // Arrange
        _sut.Properties["TestKey"] = 42;

        // Act
        var success = _sut.TryGetProperty<string>("TestKey", out var value);

        // Assert
        success.ShouldBeFalse();
        value.ShouldBeNull();
    }

    [Test]
    public void ThenSetPropertySetsValueAndReturnsBuilder()
    {
        // Act
        var result = _sut.SetProperty("TestKey", "TestValue");

        // Assert
        result.ShouldBe(_sut);
        _sut.Properties["TestKey"].ShouldBe("TestValue");
    }

    [Test]
    public void ThenSetPropertyOverwritesExistingValue()
    {
        // Arrange
        _sut.Properties["TestKey"] = "OldValue";

        // Act
        _sut.SetProperty("TestKey", "NewValue");

        // Assert
        _sut.Properties["TestKey"].ShouldBe("NewValue");
    }

    [Test]
    public void ThenGetRequiredPropertyReturnsValueWhenExists()
    {
        // Arrange
        _sut.Properties["TestKey"] = "TestValue";

        // Act
        var result = _sut.GetRequiredProperty<string>("TestKey");

        // Assert
        result.ShouldBe("TestValue");
    }

    [Test]
    public void ThenGetRequiredPropertyThrowsWhenKeyNotFound()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            _sut.GetRequiredProperty<string>("NonExistentKey"));

        exception.Message.ShouldContain("NonExistentKey");
        exception.Message.ShouldContain("was not found");
    }

    [Test]
    public void ThenGetRequiredPropertyThrowsWhenTypeMismatch()
    {
        // Arrange
        _sut.Properties["TestKey"] = 42;

        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            _sut.GetRequiredProperty<string>("TestKey"));

        exception.Message.ShouldContain("TestKey");
        exception.Message.ShouldContain("String");
    }

    [Test]
    public void ThenExtensionMethodsCanBeChained()
    {
        // Act
        var result = _sut
            .SetProperty("Key1", "Value1")
            .SetProperty("Key2", 42)
            .Messaging(m => m.WithRegion("us-east-1"));

        // Assert
        result.ShouldBe(_sut);
        _sut.GetProperty<string>("Key1").ShouldBe("Value1");
        _sut.GetProperty<int>("Key2").ShouldBe(42);
    }

    [Test]
    public void ThenGetPropertyThrowsWhenBuilderIsNull()
    {
        // Arrange
        MessagingBusBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.GetProperty<string>("key"));
    }

    [Test]
    public void ThenGetPropertyThrowsWhenKeyIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.GetProperty<string>(null));
    }

    [Test]
    public void ThenTryGetPropertyThrowsWhenBuilderIsNull()
    {
        // Arrange
        MessagingBusBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.TryGetProperty<string>("key", out _));
    }

    [Test]
    public void ThenTryGetPropertyThrowsWhenKeyIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.TryGetProperty<string>(null, out _));
    }

    [Test]
    public void ThenSetPropertyThrowsWhenBuilderIsNull()
    {
        // Arrange
        MessagingBusBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.SetProperty("key", "value"));
    }

    [Test]
    public void ThenSetPropertyThrowsWhenKeyIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.SetProperty<string>(null, "value"));
    }

    [Test]
    public void ThenGetRequiredPropertyThrowsWhenBuilderIsNull()
    {
        // Arrange
        MessagingBusBuilder builder = null;

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => builder.GetRequiredProperty<string>("key"));
    }

    [Test]
    public void ThenGetRequiredPropertyThrowsWhenKeyIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.GetRequiredProperty<string>(null));
    }
}
