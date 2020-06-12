using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenHandlingAMessageWithBinaryAttributes : IntegrationTestBase
    {
        public WhenHandlingAMessageWithBinaryAttributes(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        public class SimpleMessageWithBinaryAttributesHandler : IHandlerAsync<SimpleMessage>
        {
            private readonly IMessageContextAccessor _contextAccessor;

            public SimpleMessageWithBinaryAttributesHandler(IMessageContextAccessor contextAccessor)
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
            // Arrange
            var handler = new SimpleMessageWithBinaryAttributesHandler(new MessageContextAccessor());

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
                        .AddMessageAttribute("content", "somecontent")
                        .AddMessageAttribute("binarycontent", Encoding.UTF8.GetBytes("somebinarydata"));
                    await publisher.PublishAsync(new SimpleMessage(), metadata, cancellationToken);

                    await Patiently.AssertThatAsync(() => handler.HandledMessages.Count > 0, TimeSpan.FromSeconds(5));

                    handler.HandledMessages.Count.ShouldBe(1);
                    handler.HandledMessages[0].context.MessageAttributes.Get("content").StringValue.ShouldBe("somecontent");

                    var binaryData = handler.HandledMessages[0].context.MessageAttributes.Get("binarycontent").BinaryValue;
                    Encoding.UTF8.GetString(binaryData.ToArray()).ShouldBe("somebinarydata");
                });
        }
    }
}
