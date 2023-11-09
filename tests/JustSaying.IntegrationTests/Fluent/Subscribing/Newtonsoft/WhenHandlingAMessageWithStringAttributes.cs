using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Fluent.Subscribing.Newtonsoft;

public class WhenHandlingAMessageWithStringAttributes(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    public class SimpleMessageWithStringAttributesHandler(IMessageContextAccessor contextAccessor) : IHandlerAsync<SimpleMessage>
    {
        private readonly IMessageContextAccessor _contextAccessor = contextAccessor;

        public Task<bool> Handle(SimpleMessage message)
        {
            HandledMessages.Add((_contextAccessor.MessageContext, message));
            return Task.FromResult(true);
        }

        public List<(MessageContext context, SimpleMessage message)> HandledMessages { get; } = new List<(MessageContext, SimpleMessage)>();
    }

    [AwsFact]
    public async Task Then_The_Attributes_Are_Returned()
    {
        OutputHelper.WriteLine($"Running {nameof(Then_The_Attributes_Are_Returned)} test");

        // Arrange
        var handler = new SimpleMessageWithStringAttributesHandler(new MessageContextAccessor());

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                var metadata = new PublishMetadata()
                    .AddMessageAttribute("content1", "somecontent")
                    .AddMessageAttribute("content2", "somemorecontent");
                await publisher.PublishAsync(new SimpleMessage(), metadata, cancellationToken);

                await Patiently.AssertThatAsync(OutputHelper,
                    () => handler.HandledMessages.Count > 0, TimeSpan.FromSeconds(5));

                handler.HandledMessages.Count.ShouldBe(1);
                handler.HandledMessages[0].context.MessageAttributes.Get("content1").ShouldNotBeNull().StringValue.ShouldBe("somecontent");
                handler.HandledMessages[0].context.MessageAttributes.Get("content2").ShouldNotBeNull().StringValue.ShouldBe("somemorecontent");
            });
    }
}
