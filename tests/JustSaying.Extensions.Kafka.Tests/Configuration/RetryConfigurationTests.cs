using JustSaying.Extensions.Kafka.Configuration;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Configuration;

public class RetryConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        // Arrange & Act
        var config = new RetryConfiguration();

        // Assert
        config.Mode.ShouldBe(RetryMode.InProcess);
        config.MaxRetryAttempts.ShouldBe(3);
        config.InitialBackoff.ShouldBe(TimeSpan.FromSeconds(5));
        config.ExponentialBackoff.ShouldBeTrue();
        config.MaxBackoff.ShouldBe(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void Validate_WithValidConfiguration_ShouldNotThrow()
    {
        // Arrange
        var config = new RetryConfiguration
        {
            MaxRetryAttempts = 3,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(30)
        };

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Fact]
    public void Validate_WithNegativeMaxRetryAttempts_ShouldThrow()
    {
        // Arrange
        var config = new RetryConfiguration
        {
            MaxRetryAttempts = -1
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => config.Validate())
            .Message.ShouldContain("MaxRetryAttempts must be non-negative");
    }

    [Fact]
    public void Validate_WithNegativeInitialBackoff_ShouldThrow()
    {
        // Arrange
        var config = new RetryConfiguration
        {
            InitialBackoff = TimeSpan.FromSeconds(-1)
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => config.Validate())
            .Message.ShouldContain("InitialBackoff must be non-negative");
    }

    [Fact]
    public void Validate_WithMaxBackoffLessThanInitialBackoff_ShouldThrow()
    {
        // Arrange
        var config = new RetryConfiguration
        {
            InitialBackoff = TimeSpan.FromSeconds(30),
            MaxBackoff = TimeSpan.FromSeconds(10)
        };

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => config.Validate())
            .Message.ShouldContain("MaxBackoff must be greater than or equal to InitialBackoff");
    }

    [Fact]
    public void Validate_WithZeroMaxRetryAttempts_ShouldNotThrow()
    {
        // Arrange - Zero retry attempts is valid (no retry)
        var config = new RetryConfiguration
        {
            MaxRetryAttempts = 0,
            InitialBackoff = TimeSpan.FromSeconds(1),
            MaxBackoff = TimeSpan.FromSeconds(30)
        };

        // Act & Assert
        Should.NotThrow(() => config.Validate());
    }

    [Theory]
    [InlineData(RetryMode.InProcess)]
    [InlineData(RetryMode.TopicChaining)]
    public void Mode_CanBeSetToAnyValidValue(RetryMode mode)
    {
        // Arrange
        var config = new RetryConfiguration { Mode = mode };

        // Assert
        config.Mode.ShouldBe(mode);
    }
}

