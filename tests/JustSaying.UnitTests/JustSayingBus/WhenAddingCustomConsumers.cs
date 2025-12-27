using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenAddingCustomConsumers(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private bool _customConsumerCalled;
    private int _customConsumerCallCount;

    protected override Task WhenAsync()
    {
        // Arrange
        _customConsumerCalled = false;
        _customConsumerCallCount = 0;

        // Act
        SystemUnderTest.AddCustomConsumer(async (cancellationToken) =>
        {
            _customConsumerCalled = true;
            _customConsumerCallCount++;
            await Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ThenCustomConsumerRunsWhenBusStarts()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        await SystemUnderTest.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token); // Give custom consumer time to execute

        // Assert
        _customConsumerCalled.ShouldBeTrue();
        _customConsumerCallCount.ShouldBeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void ThenThrowsArgumentNullExceptionWhenConsumerTaskIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => SystemUnderTest.AddCustomConsumer(null));
    }
}

public class WhenAddingMultipleCustomConsumers(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private int _consumer1CallCount;
    private int _consumer2CallCount;

    protected override Task WhenAsync()
    {
        _consumer1CallCount = 0;
        _consumer2CallCount = 0;

        SystemUnderTest.AddCustomConsumer(async (cancellationToken) =>
        {
            _consumer1CallCount++;
            await Task.CompletedTask;
        });

        SystemUnderTest.AddCustomConsumer(async (cancellationToken) =>
        {
            _consumer2CallCount++;
            await Task.CompletedTask;
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ThenAllCustomConsumersRun()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act
        await SystemUnderTest.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);

        // Assert
        _consumer1CallCount.ShouldBeGreaterThanOrEqualTo(1);
        _consumer2CallCount.ShouldBeGreaterThanOrEqualTo(1);
    }
}

public class WhenAccessingMessageBodySerializerFactory(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    protected override Task WhenAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public void ThenMessageBodySerializerFactoryIsAccessible()
    {
        // Assert
        SystemUnderTest.MessageBodySerializerFactory.ShouldNotBeNull();
        SystemUnderTest.MessageBodySerializerFactory.ShouldBeOfType<NewtonsoftSerializationFactory>();
    }

    [Fact]
    public void ThenMessageBodySerializerFactoryCanBeSet()
    {
        // Arrange
        var newFactory = new NewtonsoftSerializationFactory();

        // Act
        SystemUnderTest.MessageBodySerializerFactory = newFactory;

        // Assert
        SystemUnderTest.MessageBodySerializerFactory.ShouldBe(newFactory);
    }
}

public class WhenCustomConsumerThrowsOperationCanceledException(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper)
{
    private bool _consumerStarted;

    protected override Task WhenAsync()
    {
        _consumerStarted = false;

        SystemUnderTest.AddCustomConsumer((cancellationToken) =>
        {
            _consumerStarted = true;
            throw new OperationCanceledException();
        });

        return Task.CompletedTask;
    }

    [Fact]
    public async Task ThenExceptionIsSuppressed()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert - Should not throw
        await SystemUnderTest.StartAsync(cts.Token);
        await Task.Delay(100, cts.Token);

        _consumerStarted.ShouldBeTrue();
    }
}
