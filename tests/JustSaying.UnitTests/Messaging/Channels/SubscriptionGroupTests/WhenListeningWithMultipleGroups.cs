using System.Text.Json;
using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenListeningWithMultipleGroups : BaseSubscriptionGroupTests
{
    private readonly SqsSource _queueA;
    private readonly SqsSource _queueB;

    public WhenListeningWithMultipleGroups(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        var queueA = new FakeSqsQueue(ct =>
                Task.FromResult(new List<Message>
                {
                    new TestMessage
                    {
                        Body = $$"""{"Subject":"SimpleMessage", "Message": "{{JsonEncodedText.Encode(JsonSerializer.Serialize(new SimpleMessage { Content = "Hi" }))}}" }"""
                    }
                }.AsEnumerable()),
            "EC159934-A30E-45B0-9186-78853F7D3BED");
        var queueB = new FakeSqsQueue(ct =>
                Task.FromResult(new List<Message>
                {
                    new TestMessage
                    {
                        Body = $$"""{"Subject":"SimpleMessage", "Message": "{{JsonEncodedText.Encode(JsonSerializer.Serialize(new SimpleMessage { Content = "Hi again" }))}}" }"""
                    }
                }.AsEnumerable()),
            "C7506B3F-81DA-4898-82A5-C0293523592A");

        var messageConverter = new ReceivedMessageConverter(new SystemTextJsonMessageBodySerializer<SimpleMessage>(), new MessageCompressionRegistry(), false);
        _queueA = new SqsSource
        {
            SqsQueue = queueA,
            MessageConverter = messageConverter
        };
        _queueB = new SqsSource
        {
            SqsQueue = queueB,
            MessageConverter = messageConverter
        };
    }

    protected override Dictionary<string, SubscriptionGroupConfigBuilder> SetupBusConfig()
    {
        return new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            {
                "queueA", new SubscriptionGroupConfigBuilder("queueA")
                    .AddQueue(_queueA)
                    .WithPrefetch(5)
                    .WithBufferSize(20)
                    .WithConcurrencyLimit(1)
                    .WithMultiplexerCapacity(30)
            },
            { "queueB", new SubscriptionGroupConfigBuilder("queueB").AddQueue(_queueB) }
        };
    }

    protected override void Given()
    {
        Queues.Add(_queueA);
        Queues.Add(_queueB);
    }

    [Fact]
    public void SubscriptionGroups_OverridesDefaultSettingsCorrectly()
    {
        var interrogationResult = SystemUnderTest.Interrogate();

        var json = JsonConvert.SerializeObject(interrogationResult, Formatting.Indented);

        json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
    }
}
