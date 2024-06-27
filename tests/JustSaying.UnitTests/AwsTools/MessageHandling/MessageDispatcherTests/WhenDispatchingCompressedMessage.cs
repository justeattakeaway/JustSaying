using System.IO.Compression;
using System.Text;
using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.AwsTools.MessageHandling.MessageDispatcherTests;

public class WhenDispatchingCompressedMessage
{
    [Fact]
    public async Task ShouldDecompressMessage()
    {
        // Arrange
        var originalMessage = new SimpleMessage { Id = Guid.NewGuid() };
        var messageSerializer = new MessageSerializationRegister(
            new NonGenericMessageSubjectProvider(),
            new SystemTextJsonSerializationFactory());

        messageSerializer.AddSerializer<SimpleMessage>();

        var payload = messageSerializer.Serialize(originalMessage, false);

        var memoryStream = new MemoryStream();
        await using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
        {
            gzipStream.Write(Encoding.UTF8.GetBytes(payload));
        }

        var compressedPayload = Convert.ToBase64String(memoryStream.ToArray());

        var sqsMessage = new Message
        {
            Body = compressedPayload,
            MessageAttributes =
            {
                ["Content-Encoding"] = new MessageAttributeValue { DataType = "String", StringValue = ContentEncodings.GzipBase64 }
            }
        };

        var decompressorRegistry =
            new MessageCompressionRegistry([new GzipMessageBodyCompression()]);

        var queue = new FakeSqsQueue(ct => Task.FromResult(Enumerable.Empty<Message>()));
        var queueReader = new SqsQueueReader(queue);
        var messageContext = queueReader.ToMessageContext(sqsMessage);
        var middlewareMap = new MiddlewareMap();
        var inspectableMiddleware = new InspectableMiddleware<SimpleMessage>();
        middlewareMap.Add<SimpleMessage>("fake-queue-name", inspectableMiddleware);
        var messageDispatcher = new MessageDispatcher(messageSerializer, new NullOpMessageMonitor(), middlewareMap, decompressorRegistry, new LoggerFactory());

        // Act
        await messageDispatcher.DispatchMessageAsync(messageContext, CancellationToken.None);

        // Assert
        var handledDecompressedMessage = inspectableMiddleware.Handler.ReceivedMessages.ShouldHaveSingleItem();
        handledDecompressedMessage.Id.ShouldBe(originalMessage.Id);
    }
}
