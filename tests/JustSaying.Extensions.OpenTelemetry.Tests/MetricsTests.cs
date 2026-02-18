using System.Diagnostics.Metrics;
using JustSaying.Extensions.OpenTelemetry;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.ErrorHandling;
using JustSaying.Messaging.Monitoring;
using NSubstitute;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using SqsMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Extensions.OpenTelemetry.Tests;

public class MetricsTests
{
    [Fact]
    public async Task ProcessDuration_Is_Recorded_With_Dimensional_Tags()
    {
        // Arrange
        var exportedMetrics = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedMetrics)
            .Build();

        var monitor = Substitute.For<IMessageMonitor>();
        var middleware = new StopwatchMiddleware(monitor, typeof(FakeHandler));

        var context = CreateHandleMessageContext("test-queue");

        // Act
        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);
        meterProvider.ForceFlush();

        // Assert
        var processDuration = exportedMetrics.FirstOrDefault(m => m.Name == "messaging.process.duration");
        processDuration.ShouldNotBeNull("messaging.process.duration metric should be recorded");
    }

    [Fact]
    public async Task MessagesProcessed_Is_Recorded_On_Successful_Handle()
    {
        // Arrange
        var exportedMetrics = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedMetrics)
            .Build();

        var monitor = Substitute.For<IMessageMonitor>();
        var middleware = new ErrorHandlerMiddleware(monitor);

        var context = CreateHandleMessageContext("test-queue");

        // Act
        await middleware.RunAsync(context, _ => Task.FromResult(true), CancellationToken.None);
        meterProvider.ForceFlush();

        // Assert
        var messagesProcessed = exportedMetrics.FirstOrDefault(m => m.Name == "justsaying.messages.processed");
        messagesProcessed.ShouldNotBeNull("justsaying.messages.processed metric should be recorded");
    }

    [Fact]
    public async Task MessagesProcessed_Records_ErrorType_On_Handler_Failure()
    {
        // Arrange
        var exportedMetrics = new List<Metric>();

        using var meterProvider = Sdk.CreateMeterProviderBuilder()
            .AddJustSayingInstrumentation()
            .AddInMemoryExporter(exportedMetrics)
            .Build();

        var monitor = Substitute.For<IMessageMonitor>();
        var middleware = new ErrorHandlerMiddleware(monitor);

        var context = CreateHandleMessageContext("test-queue");

        // Act - the ErrorHandlerMiddleware catches exceptions and returns false
        var result = await middleware.RunAsync(
            context,
            _ => throw new InvalidOperationException("Test handler failure"),
            CancellationToken.None);
        meterProvider.ForceFlush();

        // Assert
        result.ShouldBeFalse();
        var messagesProcessed = exportedMetrics.FirstOrDefault(m => m.Name == "justsaying.messages.processed");
        messagesProcessed.ShouldNotBeNull("justsaying.messages.processed metric should be recorded on error");
    }

    private static HandleMessageContext CreateHandleMessageContext(string queueName)
    {
        var sqsMessage = new SqsMessage { MessageId = "msg-test", Body = "{}" };
        var message = new SimpleMessage { Id = Guid.NewGuid() };
        var visibilityUpdater = Substitute.For<IMessageVisibilityUpdater>();
        var messageDeleter = Substitute.For<IMessageDeleter>();
        var queueUri = new Uri("https://sqs.eu-west-1.amazonaws.com/123456789/test-queue");
        var messageAttributes = new MessageAttributes();

        return new HandleMessageContext(
            queueName,
            sqsMessage,
            message,
            typeof(SimpleMessage),
            visibilityUpdater,
            messageDeleter,
            queueUri,
            messageAttributes);
    }

    private class SimpleMessage : JustSaying.Models.Message { }

    private class FakeHandler : IHandlerAsync<SimpleMessage>
    {
        public Task<bool> Handle(SimpleMessage message) => Task.FromResult(true);
    }
}
