using JustSaying.AwsTools.MessageHandling;
using JustSaying.IntegrationTests;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

#pragma warning disable 618

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenHandlingMultipleTopics : IntegrationTestBase
{
    [NotSimulatorSkip]
    [Test]
    public async Task Sqs_Policy_Is_Applied_With_Wildcard()
    {
        // Arrange - one queue subscribed to two topics. Both subscriptions read the same message type,
        // as two subscriptions of different types can't share a queue.
        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) => builder.Subscriptions((options) =>
            {
                options.ForTopic<TopicA>($"{UniqueName}-a", (subscription) => subscription.WithQueueName(UniqueName));
                options.ForTopic<TopicA>($"{UniqueName}-b", (subscription) => subscription.WithQueueName(UniqueName));
            }))
            .AddJustSayingHandler<TopicA, HandlerA>();

        await WhenAsync(
            services,
            async (publisher, listener, serviceProvider, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);

                var clientFactory = serviceProvider.GetRequiredService<MessagingBusBuilder>().BuildClientFactory();
                var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
                var client = clientFactory.GetSqsClient(Region);

                var queue = new SqsQueueByName(Region, UniqueName, client, 0, loggerFactory);

                await Patiently.AssertThatAsync(OutputHelper, () => queue.ExistsAsync(CancellationToken.None), 60.Seconds());

                dynamic policyJson = JObject.Parse(queue.Policy);

                ((int)policyJson.Statement.Count).ShouldBe(1, $"Expecting 1 statement in Sqs policy but found {policyJson.Statement.Count}.");
            });
    }

    private class TopicA : Message
    {
    }

    private sealed class HandlerA : IHandlerAsync<TopicA>
    {
        public Task<bool> Handle(TopicA message)
        {
            return Task.FromResult(true);
        }
    }
}