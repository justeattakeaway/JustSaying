using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;
using MessageAttributeValue = Amazon.SQS.Model.MessageAttributeValue;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests;

public class WhenDispatchingCompressedMessage
{
    [Test]
    public async Task ShouldDecompressMessage()
    {
        // Arrange
        var originalMessage = new SimpleMessage { Id = Guid.NewGuid() };
        var decompressorRegistry =
            new MessageCompressionRegistry([new GzipMessageBodyCompression()]);
        var messageConverter = new InboundMessageConverter(SimpleMessage.Serializer, decompressorRegistry, false);

        string payload = JsonSerializer.Serialize(originalMessage, originalMessage.GetType(), new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new JsonStringEnumConverter(),
            },
        });

        var memoryStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(Encoding.UTF8.GetBytes(payload));
        }

        var compressedPayload = Convert.ToBase64String(memoryStream.ToArray());
        var fullMessagePayload = JsonSerializer.Serialize(new { Subject = nameof(SimpleMessage), Message = compressedPayload });

        var sqsMessage = new Message
        {
            Body = fullMessagePayload
        };
        sqsMessage.MessageAttributes ??= [];
        sqsMessage.MessageAttributes["Content-Encoding"] = new MessageAttributeValue { DataType = "String", StringValue = ContentEncodings.GzipBase64 };

        var queue = new FakeSqsQueue(ct => Task.FromResult(Enumerable.Empty<Message>()));
        var queueReader = new SqsQueueReader(queue, messageConverter);
        var messageContext = queueReader.ToMessageContext(sqsMessage);
        var middlewareMap = new MiddlewareMap();
        var inspectableMiddleware = new InspectableMiddleware<SimpleMessage>();
        middlewareMap.Add<SimpleMessage>("fake-queue-name", inspectableMiddleware);
        var messageDispatcher = new MessageDispatcher(new NullOpMessageMonitor(), middlewareMap, new LoggerFactory());

        // Act
        await messageDispatcher.DispatchMessageAsync(messageContext, CancellationToken.None);

        // Assert
        var handledDecompressedMessage = inspectableMiddleware.Handler.ReceivedMessages.ShouldHaveSingleItem();
        handledDecompressedMessage.Id.ShouldBe(originalMessage.Id);
    }
}
