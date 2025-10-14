using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenReceivingACompressedMessage(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
    [AwsFact]
    public async Task Then_The_Message_Is_Handled()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
            .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

        var originalMessage = new SimpleMessage { Id = Guid.NewGuid() };
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

        var clientFactory = CreateClientFactory();
        var sqsClient = clientFactory.GetSqsClient(Region);

        await WhenAsync(
            services,
            async (_, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);

                var getQueueUrlResponse = await sqsClient.GetQueueUrlAsync(UniqueName, cancellationToken);
                var queueUrl = getQueueUrlResponse.QueueUrl;

                var sendMessageRequest = new SendMessageRequest
                {
                    QueueUrl = queueUrl,
                    MessageBody = fullMessagePayload,
                };
                sendMessageRequest.MessageAttributes ??= [];
                sendMessageRequest.MessageAttributes["Content-Encoding"] = new MessageAttributeValue { DataType = "String", StringValue = "gzip,base64" };

                await sqsClient.SendMessageAsync(sendMessageRequest, cancellationToken);

                await Patiently.AssertThatAsync(OutputHelper, () =>
                {
                    var receivedMessage = handler.ReceivedMessages.ShouldHaveSingleItem();
                    receivedMessage.Id.ShouldBe(originalMessage.Id);
                });
            });
    }
}
