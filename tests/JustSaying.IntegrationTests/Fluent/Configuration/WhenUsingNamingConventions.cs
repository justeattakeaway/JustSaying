using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Naming;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NSubstitute;

namespace JustSaying.Fluent.Configuration;

public class WhenUsingNamingConventions : IntegrationTestBase
{
    public WhenUsingNamingConventions(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    class TestNamingConvention : ITopicNamingConvention, IQueueNamingConvention
    {
        private readonly DefaultNamingConventions _default;

        public TestNamingConvention()
        {
            _default = new DefaultNamingConventions();
        }

        public string TopicName<T>()
        {
            return $"beforetopic-{_default.TopicName<T>()}-aftertopic";
        }

        public string QueueName<T>()
        {
            return $"beforequeue-{_default.QueueName<T>()}-afterqueue";
        }
    }

    [Fact]
    public async Task ThenTheNamingConventionIsApplied()
    {
        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder.Messaging(m =>
                m.WithQueueNamingConvention<TestNamingConvention>()
                    .WithTopicNamingConvention<TestNamingConvention>()))
            .ConfigureJustSaying((builder) =>
                builder.Publications(pub => pub.WithTopic<SimpleMessage>())
                    .Subscriptions(sub => sub.ForTopic<SimpleMessage>()))
            .AddSingleton<IHandlerAsync<SimpleMessage>, InspectableHandler<SimpleMessage>>()
            .AddSingleton<TestNamingConvention>();

        string json = "";
        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                var listenerJson = JsonConvert.SerializeObject(listener.Interrogate(), Formatting.Indented);
                var publisherJson = JsonConvert.SerializeObject(publisher.Interrogate(), Formatting.Indented);

                json = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        listenerJson,
                        publisherJson)
                    .Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));
    }
}
