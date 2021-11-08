using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

public class WhenInterrogatingTheBus : IntegrationTestBase
{
    public WhenInterrogatingTheBus(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    [AwsFact]
    public async Task Then_The_Interrogation_Result_Should_Be_Returned()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<object>();
        var handler = CreateHandler<SimpleMessage>(completionSource);

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
            {
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName);
                builder.Publications(p => p.WithTopic<SimpleMessage>());
                builder.Subscriptions(s => s.WithDefaults(x => x.WithDefaultConcurrencyLimit(10)));
            })
            .AddSingleton(handler);

        string json = "";
        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);

                var listenerJson = JsonConvert.SerializeObject(listener.Interrogate(), Formatting.Indented);
                var publisherJson = JsonConvert.SerializeObject(publisher.Interrogate(), Formatting.Indented);

                json = string.Join($"{Environment.NewLine}{Environment.NewLine}",
                        listenerJson, publisherJson)
                    .Replace(UniqueName, "integrationTestQueueName", StringComparison.Ordinal);

                completionSource.SetResult(null);
            });

        json.ShouldMatchApproved(opt => opt.SubFolder("Approvals"));
    }
}