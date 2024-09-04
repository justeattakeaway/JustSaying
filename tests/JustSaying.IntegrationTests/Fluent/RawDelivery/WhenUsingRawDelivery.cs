using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Fluent.RawDelivery;

public class WhenUsingRawDelivery(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Published()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();
        var awsClientFactory = new InspectableClientFactory(new LocalAwsClientFactory(Bus));

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
            {
                builder.Client((options) => options.WithClientFactory(() => awsClientFactory));
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    c => { c.WithReadConfiguration(rc => rc.RawMessageDelivery = true); });
            })
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        string content = Guid.NewGuid().ToString();

        var message = new SimpleMessage
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
                await Patiently.AssertThatAsync(() =>
                {
                    var handledMessage = handler.ReceivedMessages.Where(m => m.Id.ToString() == message.UniqueKey()).ShouldHaveSingleItem();
                    handledMessage.Content.ShouldBe(content);
                    var response = awsClientFactory.InspectableSqsClient.ReceiveMessageResponses.Where(r => r.Messages.Count > 0).ShouldHaveSingleItem();
                    var responseMessage = response.Messages.ShouldHaveSingleItem();
                    var messageBody = responseMessage.Body;
                    messageBody.ShouldNotContain("Message");
                    messageBody.ShouldNotContain("Subject");
                });
            });
    }
}
