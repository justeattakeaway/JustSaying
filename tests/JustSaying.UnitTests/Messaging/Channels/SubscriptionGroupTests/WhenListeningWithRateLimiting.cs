using System.Diagnostics.CodeAnalysis;
using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;

public class WhenListeningWithRateLimiting : BaseSubscriptionGroupTests
{
    [StringSyntax(StringSyntaxAttribute.Json)]
    private const string MessageBody = """
                                       {
                                         "Subject": "TestMessage",
                                         "Message": "Expected Message Body"
                                       }
                                       """;

    public WhenListeningWithRateLimiting(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        MessagesToWaitFor = 3;
    }

    protected override void Given()
    {
        var sqsSource = CreateSuccessfulTestQueue(Guid.NewGuid().ToString(), new TestMessage { Body = MessageBody });
        var queue = sqsSource.SqsQueue as FakeSqsQueue;
        queue!.MaxNumberOfMessagesToReceive = MessagesToWaitFor;

        Queues.Add(sqsSource);
    }

    protected override Dictionary<string, SubscriptionGroupConfigBuilder> SetupBusConfig()
    {
        return new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            ["test"] = new SubscriptionGroupConfigBuilder("test")
                .AddQueues(Queues)
                .WithConcurrencyLimit(100, ConcurrencyLimitType.MessagesPerSecond)
        };
    }

    [Fact]
    public void MessagesAreHandledSuccessfully()
    {
        Handler.ReceivedMessages.ShouldNotBeEmpty();
    }

    [Fact]
    public void InterrogationShowsMessagesPerSecondLimitType()
    {
        var interrogationResult = SystemUnderTest.Interrogate();
        var json = Newtonsoft.Json.JsonConvert.SerializeObject(interrogationResult);

        json.ShouldContain("\"ConcurrencyLimitType\":\"MessagesPerSecond\"");
    }
}
