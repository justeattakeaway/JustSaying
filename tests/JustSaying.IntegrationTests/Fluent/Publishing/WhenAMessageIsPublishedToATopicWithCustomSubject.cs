using System.Text.Json.Nodes;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenAMessageIsPublishedToATopicWithCustomSubject(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();
        var awsClientFactory = new InspectableClientFactory(new LocalAwsClientFactory(Bus));

        var services = GivenJustSaying()
            .ConfigureJustSaying(
                (builder) =>
                {
                    builder.Client((options) => options.WithClientFactory(() => awsClientFactory));
                    builder.WithLoopbackTopicAndPublicationOptions<SimpleMessage>(UniqueName, configure => configure.WithWriteConfiguration(wc => wc.Subject = "RandomSubject"));
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
                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        handler.ReceivedMessages.ShouldHaveSingleItem().Content.ShouldBe(content);
                    });

                var response = awsClientFactory.InspectableSqsClient.ReceiveMessageResponses.Where(r => r.Messages.Count > 0).ShouldHaveSingleItem();
                var responseMessage = response.Messages.ShouldHaveSingleItem();
                string messageBody = responseMessage.Body;
                var bodyJson = JsonNode.Parse(messageBody);
                bodyJson.ShouldNotBeNull();
                bodyJson["Subject"].ShouldNotBeNull().GetValue<string>().ShouldBe("RandomSubject");
            });
    }
}
