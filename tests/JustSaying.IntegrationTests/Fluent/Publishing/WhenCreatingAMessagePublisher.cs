using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenCreatingAMessagePublisher(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Queues_Exist()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Publications((options) =>
                options.WithQueue<SimpleMessage>(qo => qo.WithName(UniqueName))))
            .BuildServiceProvider();

        // Act - Force queue creation
        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        await publisher.StartAsync(CancellationToken.None);

        // Assert
        var client = CreateClientFactory().GetSqsClient(Region);

        var queues = await client.ListQueuesAsync(UniqueName);
        queues.QueueUrls.Count.ShouldBe(2, "An incorrect number of queues were created.");
        queues.QueueUrls.ShouldAllBe((url) => url.Contains(UniqueName, StringComparison.Ordinal), "The queue URL is not for the expected queue.");
        queues.QueueUrls.Count((url) => url.Contains("_error", StringComparison.Ordinal)).ShouldBe(1, "The error queue was not created.");
    }
}