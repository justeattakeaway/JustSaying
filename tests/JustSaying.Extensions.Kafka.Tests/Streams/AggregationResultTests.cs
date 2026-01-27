using JustSaying.Extensions.Kafka.Streams;
using Shouldly;

namespace JustSaying.Extensions.Kafka.Tests.Streams;

public class AggregationResultTests
{
    #region AggregationResult Tests

    [Fact]
    public void AggregationResult_CanSetKeyAndValue()
    {
        // Arrange & Act
        var result = new AggregationResult<string, int>
        {
            Key = "customer-123",
            Value = 42
        };

        // Assert
        result.Key.ShouldBe("customer-123");
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void AggregationResult_InheritsFromMessage()
    {
        // Arrange
        var result = new AggregationResult<string, long>();

        // Assert
        result.ShouldBeAssignableTo<JustSaying.Models.Message>();
    }

    [Fact]
    public void AggregationResult_SupportsComplexKeyTypes()
    {
        // Arrange & Act
        var result = new AggregationResult<(string Region, string Product), decimal>
        {
            Key = ("US", "Widget"),
            Value = 1234.56m
        };

        // Assert
        result.Key.Region.ShouldBe("US");
        result.Key.Product.ShouldBe("Widget");
        result.Value.ShouldBe(1234.56m);
    }

    #endregion

    #region WindowedAggregationResult Tests

    [Fact]
    public void WindowedAggregationResult_CanSetAllProperties()
    {
        // Arrange
        var windowStart = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var windowEnd = new DateTime(2024, 1, 1, 13, 0, 0, DateTimeKind.Utc);

        // Act
        var result = new WindowedAggregationResult<string, int>
        {
            Key = "customer-456",
            Value = 100,
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };

        // Assert
        result.Key.ShouldBe("customer-456");
        result.Value.ShouldBe(100);
        result.WindowStart.ShouldBe(windowStart);
        result.WindowEnd.ShouldBe(windowEnd);
    }

    [Fact]
    public void WindowedAggregationResult_InheritsFromMessage()
    {
        // Arrange
        var result = new WindowedAggregationResult<string, long>();

        // Assert
        result.ShouldBeAssignableTo<JustSaying.Models.Message>();
    }

    [Fact]
    public void WindowedAggregationResult_WindowDurationCanBeCalculated()
    {
        // Arrange
        var windowStart = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var windowEnd = new DateTime(2024, 1, 1, 12, 5, 0, DateTimeKind.Utc);

        var result = new WindowedAggregationResult<string, int>
        {
            WindowStart = windowStart,
            WindowEnd = windowEnd
        };

        // Act
        var duration = result.WindowEnd - result.WindowStart;

        // Assert
        duration.ShouldBe(TimeSpan.FromMinutes(5));
    }

    #endregion

    #region WindowType Tests

    [Fact]
    public void WindowType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<WindowType>().Length.ShouldBe(3);
        WindowType.Tumbling.ShouldBe((WindowType)0);
        WindowType.Sliding.ShouldBe((WindowType)1);
        WindowType.Session.ShouldBe((WindowType)2);
    }

    #endregion
}
