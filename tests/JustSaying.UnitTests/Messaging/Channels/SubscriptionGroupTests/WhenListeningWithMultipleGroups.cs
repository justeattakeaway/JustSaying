using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using Newtonsoft.Json;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenListeningWithMultipleGroups(ITestOutputHelper testOutputHelper) : BaseSubscriptionGroupTests(testOutputHelper)
{
    private readonly ISqsQueue _queueB = CreateSuccessfulTestQueue("C7506B3F-81DA-4898-82A5-C0293523592A", new TestMessage());
    private readonly ISqsQueue _queueA = CreateSuccessfulTestQueue("EC159934-A30E-45B0-9186-78853F7D3BED", new TestMessage());

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