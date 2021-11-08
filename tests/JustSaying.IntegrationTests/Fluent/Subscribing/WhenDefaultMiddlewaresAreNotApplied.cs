using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenDefaultMiddlewaresAreNotApplied : IntegrationTestBase
{
    public WhenDefaultMiddlewaresAreNotApplied(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    [AwsFact]
    public async Task Then_The_Pipeline_Should_Only_Contain_User_Specified_Middlewares()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();
        var testMiddleware = new InspectableMiddleware<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                MessagingBusBuilderTestExtensions.WithLoopbackTopic<SimpleMessage>(builder,
                    UniqueName,
                    c => c.WithMiddlewareConfiguration(m =>
                    {
                        m.Use(testMiddleware);
                        m.UseHandler(_ => handler);
                    })))
            .AddJustSayingHandlers(new[] { handler });

        string json = "";
        await WhenAsync(
            services,
            (_, listener, _, _) =>
            {
                dynamic middlewares = ((dynamic)listener.Interrogate().Data).Middleware;

                json = JsonConvert.SerializeObject(middlewares, Formatting.Indented)
                    .Replace(UniqueName, "TestQueueName");

                return Task.CompletedTask;
            });

        json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
    }
}