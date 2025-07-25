using Amazon.SQS.Model;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Newtonsoft.Json;

namespace JustSaying.UnitTests.JustSayingBus;

public sealed class WhenRegisteringSubscribers(ITestOutputHelper outputHelper) : GivenAServiceBus(outputHelper), IDisposable
{
    private FakeSqsQueue _queue1;
    private FakeSqsQueue _queue2;
    private CancellationTokenSource _cts;

    protected override void Given()
    {
        base.Given();

        static IEnumerable<Message> GetMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                yield return new TestMessage();
            }
        }

        _queue1 = new FakeSqsQueue(ct =>Task.FromResult(GetMessages(ct)), "queue1");
        _queue2 = new FakeSqsQueue(ct => Task.FromResult(GetMessages(ct)), "queue2");
    }

    protected override async Task WhenAsync()
    {
        SystemUnderTest.AddMessageMiddleware<OrderAccepted>(_queue1.QueueName,
            new InspectableMiddleware<OrderAccepted>());
        SystemUnderTest.AddMessageMiddleware<OrderRejected>(_queue1.QueueName,
            new InspectableMiddleware<OrderRejected>());
        SystemUnderTest.AddMessageMiddleware<SimpleMessage>(_queue1.QueueName,
            new InspectableMiddleware<SimpleMessage>());

        var messageConverter1 = new InboundMessageConverter(new SystemTextJsonMessageBodySerializer<OrderAccepted>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions), new MessageCompressionRegistry(), false);
        var messageConverter2 = new InboundMessageConverter(new SystemTextJsonMessageBodySerializer<OrderRejected>(SystemTextJsonMessageBodySerializer.DefaultJsonSerializerOptions), new MessageCompressionRegistry(), false);
        SystemUnderTest.AddQueue("groupA", new SqsSource
        {
            SqsQueue = _queue1,
            MessageConverter = messageConverter1
        });
        SystemUnderTest.AddQueue("groupB", new SqsSource
        {
            SqsQueue = _queue2,
            MessageConverter = messageConverter2
        });

        _cts = new CancellationTokenSource();
        _cts.CancelAfter(TimeSpan.FromSeconds(5));

        await SystemUnderTest.StartAsync(_cts.Token);
    }

    [Fact]
    public async Task SubscribersStartedUp()
    {
        await Patiently.AssertThatAsync(OutputHelper,
            () =>
            {
                _queue1.ReceiveMessageRequests.Count.ShouldBeGreaterThan(0);
                _queue2.ReceiveMessageRequests.Count.ShouldBeGreaterThan(0);
            });
    }

    [Fact]
    public void AndInterrogationShowsSubscribersHaveBeenSet()
    {
        dynamic response = SystemUnderTest.Interrogate();

        string json = JsonConvert.SerializeObject(response.Data.Middleware.Data.Middlewares, Formatting.Indented);

        json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
    }

    private class TestMessage : Message
    {
        public TestMessage()
        {
            Body = "TestMessage";
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
    }
}
