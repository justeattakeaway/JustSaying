using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.JustSayingBus;

public class WhenAddingCustomConsumers : GivenAServiceBus
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

    [Test]
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

    [Test]
    public void ThenThrowsArgumentNullExceptionWhenConsumerTaskIsNull()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => SystemUnderTest.AddCustomConsumer(null));
    }
}

public class WhenAddingMultipleCustomConsumers : GivenAServiceBus
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

    [Test]
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

public class WhenAccessingMessageBodySerializerFactory : GivenAServiceBus
{
    protected override Task WhenAsync()
    {
        return Task.CompletedTask;
    }

    [Test]
    public void ThenMessageBodySerializerFactoryIsAccessible()
    {
        // Assert
        SystemUnderTest.MessageBodySerializerFactory.ShouldNotBeNull();
        SystemUnderTest.MessageBodySerializerFactory.ShouldBeOfType<NewtonsoftSerializationFactory>();
    }

    [Test]
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

public class WhenCustomConsumerThrowsOperationCanceledException : GivenAServiceBus
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

    [Test]
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

public class WhenCustomConsumerThrowsGeneralException : GivenAServiceBus
{
    private bool _consumerStarted;
    private bool _otherConsumerCompleted;

    protected override Task WhenAsync()
    {
        _consumerStarted = false;
        _otherConsumerCompleted = false;

        // Add a consumer that throws a general exception
        SystemUnderTest.AddCustomConsumer((cancellationToken) =>
        {
            _consumerStarted = true;
            throw new InvalidOperationException("Test exception from custom consumer");
        });

        // Add another consumer to verify the bus continues running
        SystemUnderTest.AddCustomConsumer(async (cancellationToken) =>
        {
            await Task.Delay(50, cancellationToken);
            _otherConsumerCompleted = true;
        });

        return Task.CompletedTask;
    }

    [Test]
    public async Task ThenExceptionIsSuppressedAndOtherConsumersContinue()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        // Act & Assert - Should not throw
        await SystemUnderTest.StartAsync(cts.Token);
        await Task.Delay(200, cts.Token);

        _consumerStarted.ShouldBeTrue();
        _otherConsumerCompleted.ShouldBeTrue();
    }
}

public class WhenAddingCustomConsumerAfterBusStarted : GivenAServiceBus
{
    protected override Task WhenAsync()
    {
        return Task.CompletedTask;
    }

    [Test]
    public async Task ThenThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        await SystemUnderTest.StartAsync(cts.Token);

        // Act & Assert
        Should.Throw<InvalidOperationException>(() =>
            SystemUnderTest.AddCustomConsumer(async (ct) => await Task.CompletedTask))
            .Message.ShouldContain("Cannot add custom consumers after the bus has been started");
    }
}
