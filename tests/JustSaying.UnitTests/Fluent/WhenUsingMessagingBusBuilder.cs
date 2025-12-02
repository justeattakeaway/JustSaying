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

    [Fact]
    public void ThenPropertiesDictionaryIsInitialized()
    {
        // Assert
        _sut.Properties.ShouldNotBeNull();
        _sut.Properties.ShouldBeEmpty();
    }

    [Fact]
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

    [Fact]
    public void ThenMessagingConfigIsAccessible()
    {
        // Assert
        _sut.MessagingConfig.ShouldNotBeNull();
    }

    [Fact]
    public void ThenMessagingConfigCanBeSet()
    {
        // Arrange
        var newConfig = new MessagingConfigurationBuilder(_sut);

        // Act
        _sut.MessagingConfig = newConfig;

        // Assert
        _sut.MessagingConfig.ShouldBe(newConfig);
    }

    [Fact]
    public void ThenClientConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Client(null));
    }

    [Fact]
    public void ThenClientConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Client(c => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenMessagingConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Messaging(null));
    }

    [Fact]
    public void ThenMessagingConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Messaging(m => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenPublicationsConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Publications(null));
    }

    [Fact]
    public void ThenPublicationsConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Publications(p => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenServicesConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Services(null));
    }

    [Fact]
    public void ThenServicesConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Services(s => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenSubscriptionsConfigurationThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Subscriptions(null));
    }

    [Fact]
    public void ThenSubscriptionsConfigurationReturnsBuilder()
    {
        // Act
        var result = _sut.Subscriptions(s => { });

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
    public void ThenWithServiceResolverThrowsWhenNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.WithServiceResolver(null));
    }

    [Fact]
    public void ThenWithServiceResolverReturnsBuilder()
    {
        // Arrange
        var resolver = NSubstitute.Substitute.For<IServiceResolver>();

        // Act
        var result = _sut.WithServiceResolver(resolver);

        // Assert
        result.ShouldBe(_sut);
    }

    [Fact]
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

    [Fact]
    public void ThenPropertiesCanBeUsedForExtensions()
    {
        // Arrange
        var kafkaConfig = new { BootstrapServers = "localhost:9092" };

        // Act
        _sut.Properties["KafkaConfig"] = kafkaConfig;

        // Assert
        _sut.Properties["KafkaConfig"].ShouldBe(kafkaConfig);
    }
}
