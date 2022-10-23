using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenSubscribingToMultipleTopics : IntegrationTestBase
{
    public WhenSubscribingToMultipleTopics(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    [AwsFact]
    public async Task Then_Both_Handlers_Receive_Messages()
    {
        // Arrange
        var genericHandler = new InspectableHandler<GenericMessage<SimpleMessage>>();
        var nonGenericHandler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<GenericMessage<SimpleMessage>>($"{UniqueName}-generic"))
            .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>($"{UniqueName}-nongeneric"))
            .AddSingleton<IHandlerAsync<GenericMessage<SimpleMessage>>>(genericHandler)
            .AddSingleton<IHandlerAsync<SimpleMessage>>(nonGenericHandler);

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                await publisher.PublishAsync(new GenericMessage<SimpleMessage>(), cancellationToken);
                await publisher.PublishAsync(new SimpleMessage(), cancellationToken);

                await Patiently.AssertThatAsync(OutputHelper,
                    () =>
                    {
                        genericHandler.ReceivedMessages.ShouldHaveSingleItem();
                        nonGenericHandler.ReceivedMessages.ShouldHaveSingleItem();
                    });
            });
    }
}
