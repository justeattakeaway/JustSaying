using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.TestingFramework;
using Shouldly;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently;

[Collection(GlobalSetup.CollectionName)]
public class WhenAMessageIsPublishedWithCompression : GivenANotificationStack
{
    private Future<SimpleMessage> _handler;
    private SimpleMessage _originalMessage;

    protected override void Given()
    {
        base.Given();

        _handler = new Future<SimpleMessage>
        {
            ExpectedMessageCount = 1
        };
        RegisterSnsHandler(_handler);
    }

    protected override async Task When()
    {
        _originalMessage = new SimpleMessage { Id = Guid.NewGuid() };
        string payload = JsonSerializer.Serialize(_originalMessage, _originalMessage.GetType(), new JsonSerializerOptions
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

        var sqsClient = ((JustSaying.JustSayingFluently)ServiceBus).AwsClientFactory.GetSqsClient(TestEnvironment.Region);
        var getQueueUrlResponse = await sqsClient.GetQueueUrlAsync(UniqueName);
        var queueUrl = getQueueUrlResponse.QueueUrl;

        var sendMessageRequest = new SendMessageRequest
        {
            QueueUrl = queueUrl,
            MessageBody = fullMessagePayload,
        };
        sendMessageRequest.MessageAttributes ??= [];
        sendMessageRequest.MessageAttributes["Content-Encoding"] = new MessageAttributeValue { DataType = "String", StringValue = "gzip,base64" };

        await sqsClient.SendMessageAsync(sendMessageRequest);
    }

    [AwsFact]
    public async Task ThenItGetsHandled()
    {
        var done = await Tasks.WaitWithTimeoutAsync(_handler.DoneSignal);
        done.ShouldBeTrue();

        _handler.ReceivedMessageCount.ShouldBeGreaterThanOrEqualTo(1);
        _handler.HasReceived(_originalMessage).ShouldBeTrue();
    }
}
