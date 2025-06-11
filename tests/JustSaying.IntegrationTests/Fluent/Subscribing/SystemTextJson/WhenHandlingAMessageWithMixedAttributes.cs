using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.Fluent.Subscribing.SystemTextJson;

public class WhenHandlingAMessageWithMixedAttributes(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    public class SimpleMessageWithMixedAttributesHandler(IMessageContextAccessor contextAccessor) : IHandlerAsync<SimpleMessage>
    {
        private readonly IMessageContextAccessor _contextAccessor = contextAccessor;

        public Task<bool> Handle(SimpleMessage message)
        {
            HandledMessages.Add((_contextAccessor.MessageContext, message));
            return Task.FromResult(true);
        }

        public List<(MessageContext context, SimpleMessage message)> HandledMessages { get; } = [];
    }

    [AwsFact]
    public async Task Then_The_Attributes_Are_Returned()
    {
        OutputHelper.WriteLine($"Running {nameof(Then_The_Attributes_Are_Returned)} test");

        // Arrange
        var handler = new SimpleMessageWithMixedAttributesHandler(new MessageContextAccessor());

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder
                .WithLoopbackTopic<SimpleMessage>(UniqueName))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act
                var metadata = new PublishMetadata()
                    .AddMessageAttribute("Text", "foo")
                    .AddMessageAttribute("Integer", 42)
                    .AddMessageAttribute("BinaryData", [.. "SnVzdCBFYXQgVGFrZWF3YXkuY29t"u8])
                    .AddMessageAttribute("CustomBinaryData", new MessageAttributeValue
                    {
                        DataType = "Binary.jet",
                        BinaryValue = [ .."SnVzdFNheWluZw=="u8]
                    });
                await publisher.PublishAsync(new SimpleMessage(), metadata, cancellationToken);

                await Patiently.AssertThatAsync(OutputHelper, () => handler.HandledMessages.Count > 0);

                handler.HandledMessages.Count.ShouldBe(1);
                var textAttribute = handler.HandledMessages[0].context.MessageAttributes.Get("Text");
                textAttribute.DataType.ShouldBe("String");
                textAttribute.StringValue.ShouldBe("foo");

                var integerAttribute = handler.HandledMessages[0].context.MessageAttributes.Get("Integer");
                integerAttribute.DataType.ShouldBe("Number");
                integerAttribute.StringValue.ShouldBe("42");

                var binaryDataAttribute = handler.HandledMessages[0].context.MessageAttributes.Get("BinaryData");
                binaryDataAttribute.DataType.ShouldBe("Binary");
                binaryDataAttribute.BinaryValue.ShouldBe([.. "SnVzdCBFYXQgVGFrZWF3YXkuY29t"u8]);

                var customBinaryDataAttribute = handler.HandledMessages[0].context.MessageAttributes.Get("CustomBinaryData");
                customBinaryDataAttribute.DataType.ShouldBe("Binary.jet");
                customBinaryDataAttribute.StringValue.ShouldBe(null);
                customBinaryDataAttribute.BinaryValue.ShouldBe([.. "SnVzdFNheWluZw=="u8]);
            });
    }
}
