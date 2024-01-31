using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenDefaultMiddlewaresAreNotApplied(ITestOutputHelper outputHelper) : IntegrationTestBase(outputHelper)
{
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
                        m.UseHandler<SimpleMessage>(new DummyHandlerResolver<SimpleMessage>(handler));
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
