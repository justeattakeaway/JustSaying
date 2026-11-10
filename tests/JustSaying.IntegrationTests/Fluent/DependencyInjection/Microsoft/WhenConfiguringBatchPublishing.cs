using JustSaying.AwsTools.MessageHandling;
using JustSaying.Fluent;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.DependencyInjection.Microsoft;

public class WhenConfiguringBatchPublishing : IntegrationTestBase
{
    [Test]
    public void Then_The_Batch_Settings_Are_Applied_Without_Changing_The_Single_Message_Settings()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Messaging((options) => options
                .WithPublishFailureReattempts(2)
                .WithPublishFailureBackoff(TimeSpan.FromMilliseconds(20))
                .WithPublishFailureReattemptsForBatch(7)
                .WithPublishFailureBackoffForBatch(TimeSpan.FromMilliseconds(70))))
            .BuildServiceProvider();

        // Act
        var publisher = serviceProvider.GetRequiredService<IMessageBatchPublisher>();

        // Assert
        var bus = publisher.ShouldBeOfType<JustSayingBus>();

        bus.PublishBatchConfiguration.PublishFailureReAttempts.ShouldBe(7);
        bus.PublishBatchConfiguration.PublishFailureBackoff.ShouldBe(TimeSpan.FromMilliseconds(70));
        bus.Config.PublishFailureReAttempts.ShouldBe(2);
        bus.Config.PublishFailureBackoff.ShouldBe(TimeSpan.FromMilliseconds(20));
    }

    [Test]
    public void Then_The_Batch_Settings_Fall_Back_To_The_Single_Message_Settings()
    {
        // Arrange
        var serviceProvider = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Messaging((options) => options
                .WithPublishFailureReattempts(2)
                .WithPublishFailureBackoff(TimeSpan.FromMilliseconds(20))))
            .BuildServiceProvider();

        // Act
        var bus = serviceProvider.GetRequiredService<IMessageBatchPublisher>().ShouldBeOfType<JustSayingBus>();

        // Assert
        bus.PublishBatchConfiguration.PublishFailureReAttempts.ShouldBe(2);
        bus.PublishBatchConfiguration.PublishFailureBackoff.ShouldBe(TimeSpan.FromMilliseconds(20));
    }

    [Test]
    public async Task Then_The_Batch_Response_Logger_Is_Called()
    {
        // Arrange
        var batchResponses = new List<MessageBatchResponse>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder
                .Messaging((options) => options.WithMessageResponseLogger((MessageBatchResponse response, IReadOnlyCollection<object> _) => batchResponses.Add(response)))
                .Publications((options) => options.WithQueue<SimpleMessage>(QueueDestination.Named(UniqueName))));

        await WhenBatchAsync(
            services,
            async (publisher, _, cancellationToken) =>
            {
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishBatchAsync([new SimpleMessage(), new SimpleMessage()], cancellationToken);
            });

        // Assert
        batchResponses.Count.ShouldBe(1);
    }
}
