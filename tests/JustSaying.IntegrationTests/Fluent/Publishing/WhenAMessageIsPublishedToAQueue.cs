using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToAQueue(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddSingleton(handler);

        string content = Guid.NewGuid().ToString();

        var message = new SimpleMessage()
        {
            Content = content
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, cancellationToken);

                // Assert
                completionSource.Task.Wait(cancellationToken);

                await handler.Received().Handle(Arg.Is<SimpleMessage>((m) => m.Content == content));
            });
    }

    [AwsTheory]
    [InlineData(10, 10)]
    [InlineData(10, 100)]
    [InlineData(5, 100)]
    public async Task Then_Multiple_Messages_Are_Handled(int maxBatchSize, int batchSize)
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource, batchSize);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddSingleton(handler);

        var messages = new List<Message>();
        for (int i = 0; i < batchSize; i++)
        {
            messages.Add(new SimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            });
        }

        await WhenBatchAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(messages, new PublishBatchMetadata
                {
                    BatchSize = maxBatchSize
                }, cancellationToken);

                // Assert
                completionSource.Task.Wait(cancellationToken);

                await handler.Received(batchSize).Handle(Arg.Is<SimpleMessage>((m) => messages.Any(y => y.Id == m.Id)));
            });
    }

    [AwsTheory]
    [InlineData(10, 10)]
    [InlineData(10, 100)]
    [InlineData(5, 100)]
    public async Task Then_Multiple_Message_Types_Are_Handled(int maxBatchSize, int batchSize)
    {
        // Arrange
        var completionSource1 = new TaskCompletionSource<object>();
        var handler1 = CreateHandler<SimpleMessage>(completionSource1, batchSize);

        var completionSource2 = new TaskCompletionSource<object>();
        var handler2 = CreateHandler<AnotherSimpleMessage>(completionSource2, batchSize);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
            {
                builder.WithLoopbackQueue<SimpleMessage>(UniqueName);
                builder.WithLoopbackQueue<AnotherSimpleMessage>(UniqueName + "ish");
            })
            .AddSingleton(handler1)
            .AddSingleton(handler2);

        var messages = new List<Message>();
        for (int i = 0; i < batchSize; i++)
        {
            messages.Add(new SimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            });
        }

        for (int i = 0; i < batchSize; i++)
        {
            messages.Add(new AnotherSimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            });
        }

        await WhenBatchAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(messages, new PublishBatchMetadata
                {
                    BatchSize = maxBatchSize
                }, cancellationToken);

                // Assert
                completionSource1.Task.Wait(cancellationToken);
                await handler1.Received(batchSize).Handle(Arg.Is<SimpleMessage>((m) => messages.Any(y => y.Id == m.Id)));

                completionSource2.Task.Wait(cancellationToken);
                await handler2.Received(batchSize).Handle(Arg.Is<AnotherSimpleMessage>((m) => messages.Any(y => y.Id == m.Id)));
            });
    }
}
