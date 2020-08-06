using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.Fluent.Subscribing.Newtonsoft
{
    public class WhenHandlingAMessageWithStringAttributes : IntegrationTestBase
    {
        public WhenHandlingAMessageWithStringAttributes(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        public class SimpleMessageWithStringAttributesHandler : IHandlerAsync<SimpleMessage>
        {
            private readonly IMessageContextAccessor _contextAccessor;

            public SimpleMessageWithStringAttributesHandler(IMessageContextAccessor contextAccessor)
            {
                _contextAccessor = contextAccessor;
                HandledMessages = new List<(MessageContext, SimpleMessage)>();
            }
            public Task<bool> Handle(SimpleMessage message)
            {
                HandledMessages.Add((_contextAccessor.MessageContext, message));
                return Task.FromResult(true);
            }

            public List<(MessageContext context, SimpleMessage message)> HandledMessages { get; }
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
                    _ = listener.StartAsync(cancellationToken);

                    // Act
                    var metadata = new PublishMetadata()
                        .AddMessageAttribute("content1", "somecontent")
                        .AddMessageAttribute("content2", "somemorecontent");
                    await publisher.PublishAsync(new SimpleMessage(), metadata, cancellationToken);

                    await Patiently.AssertThatAsync(OutputHelper, () => handler.HandledMessages.Count > 0, TimeSpan.FromSeconds(5));

                    handler.HandledMessages.Count.ShouldBe(1);
                    handler.HandledMessages[0].context.MessageAttributes.Get("content1").StringValue.ShouldBe("somecontent");
                    handler.HandledMessages[0].context.MessageAttributes.Get("content2").StringValue.ShouldBe("somemorecontent");
                });
        }
    }
}
