using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenTheErrorQueueDisabled : IntegrationTestBase
{
    public WhenTheErrorQueueDisabled(ITestOutputHelper outputHelper)
        : base(outputHelper)
    {
    }

    [AwsFact]
    public async Task Then_The_Error_Queue_Does_Not_Exist()
    {
        await ThenTheErrorQueueDoesNotExist<IMessagePublisher>();
        await ThenTheErrorQueueDoesNotExist<IMessageBatchPublisher>();
    }

    private async Task ThenTheErrorQueueDoesNotExist<T>()
        where  T: IStartable
    {

        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource);

        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying(
                (builder) => builder.Publications(
                    (options) => options.WithQueue<SimpleMessage>(
                        (queue) => queue.WithWriteConfiguration(
                            (config) => config.WithQueueName(UniqueName)
                                .WithNoErrorQueue()))))
            .AddSingleton(handler)
            .BuildServiceProvider();

        // Act - Force queue creation
        var publisher = serviceProvider.GetRequiredService<T>();
        await publisher.StartAsync(CancellationToken.None);

        // Assert
        var client = CreateClientFactory().GetSqsClient(Region);

        var queues = await client.ListQueuesAsync(UniqueName);
        queues.QueueUrls.Count.ShouldBe(1, "More than one queue was created.");
        queues.QueueUrls.ShouldAllBe((url) => url.Contains(UniqueName, StringComparison.Ordinal), "The queue URL is not for the expected queue.");
        queues.QueueUrls.ShouldAllBe((url) => !url.Contains("_error", StringComparison.Ordinal), "The queue URL appears to be for an error queue.");
    }
}
