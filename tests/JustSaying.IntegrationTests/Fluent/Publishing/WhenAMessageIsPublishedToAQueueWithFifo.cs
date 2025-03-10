using JustSaying.Messaging;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToAQueueWithFifo(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource);
        var uniqueFifoName = UniqueName + ".fifo";

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackFifoQueue<SimpleMessage>(uniqueFifoName))
            .AddSingleton(handler);

        string content = Guid.NewGuid().ToString();
        string messageGroupId = Guid.NewGuid().ToString();
        string messageDeduplicationId = Guid.NewGuid().ToString();

        var message = new SimpleMessage()
        {
            Content = content,
        };

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(message, new PublishMetadata().AddMessageGroupId(messageGroupId).AddMessageDeduplicationId(messageDeduplicationId), cancellationToken);

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
        var uniqueFifoName = UniqueName + ".fifo";

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackFifoQueue<SimpleMessage>(uniqueFifoName))
            .AddSingleton(handler);

        var messages = new List<Message>();
        var messageGroupIds = new Dictionary<Message, string>();
        var messageDeduplicationIds = new Dictionary<Message, string>();
        for (int i = 0; i < batchSize; i++)
        {
            var message = new SimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            };

            messages.Add(message);
            messageGroupIds.Add(message, Guid.NewGuid().ToString());
            messageDeduplicationIds.Add(message, Guid.NewGuid().ToString());
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
                    BatchSize = maxBatchSize,
                    MessageGroupIds = messageGroupIds,
                    MessageDeduplicationIds = messageDeduplicationIds,
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
        var uniqueFifoNam1 = UniqueName + ".fifo";

        var completionSource2 = new TaskCompletionSource<object>();
        var handler2 = CreateHandler<AnotherSimpleMessage>(completionSource2, batchSize);
        var uniqueFifoName2 = UniqueName + "ish" + ".fifo";

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
            {
                builder.WithLoopbackFifoQueue<SimpleMessage>(uniqueFifoNam1);
                builder.WithLoopbackFifoQueue<AnotherSimpleMessage>(uniqueFifoName2);
            })
            .AddSingleton(handler1)
            .AddSingleton(handler2);

        var messages = new List<Message>();
        var messageGroupIds = new Dictionary<Message, string>();
        var messageDeduplicationIds = new Dictionary<Message, string>();
        for (int i = 0; i < batchSize; i++)
        {
            var message = new SimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            };

            messages.Add(message);
            messageGroupIds.Add(message, Guid.NewGuid().ToString());
            messageDeduplicationIds.Add(message, Guid.NewGuid().ToString());
        }

        for (int i = 0; i < batchSize; i++)
        {
            var message = new AnotherSimpleMessage
            {
                Content = $"Message {i} of {batchSize} with max batch size {maxBatchSize}"
            };

            messages.Add(message);
            messageGroupIds.Add(message, Guid.NewGuid().ToString());
            messageDeduplicationIds.Add(message, Guid.NewGuid().ToString());
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
                    BatchSize = maxBatchSize,
                    MessageGroupIds = messageGroupIds,
                    MessageDeduplicationIds = messageDeduplicationIds,
                }, cancellationToken);

                // Assert
                completionSource1.Task.Wait(cancellationToken);
                await handler1.Received(batchSize).Handle(Arg.Is<SimpleMessage>((m) => messages.Any(y => y.Id == m.Id)));

                completionSource2.Task.Wait(cancellationToken);
                await handler2.Received(batchSize).Handle(Arg.Is<AnotherSimpleMessage>((m) => messages.Any(y => y.Id == m.Id)));
            });
    }
}
