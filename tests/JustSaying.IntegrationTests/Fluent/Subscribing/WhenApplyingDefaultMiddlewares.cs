using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace JustSaying.IntegrationTests.Fluent.Subscribing;

public class WhenApplyingDefaultMiddlewares : IntegrationTestBase
{
    public WhenApplyingDefaultMiddlewares(ITestOutputHelper outputHelper) : base(outputHelper)
    { }

    class OuterTestMiddleware : InspectableMiddleware<SimpleMessage>
    { }

    class InnerTestMiddleware : InspectableMiddleware<SimpleMessage>
    { }

    class AfterTestMiddleware : InspectableMiddleware<SimpleMessage>
    { }

    [AwsFact]
    public async Task Then_The_Defaults_Are_The_Defaults_For_Sure()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
            .AddJustSayingHandlers(new[] { handler });

        string json = "";
        await WhenAsync(
            services,
            (_, listener, _, _) =>
            {
                dynamic interrogation = listener.Interrogate();
                dynamic middlewares = interrogation.Data.Middleware;

                json = JsonConvert.SerializeObject(middlewares, Formatting.Indented)
                    .Replace(UniqueName, "TestQueueName");

                return Task.CompletedTask;
            });

        json.ShouldMatchApproved(c => c.SubFolder("Approvals"));
    }

    [AwsFact]
    public async Task Then_The_Builder_Should_Put_User_Middlewares_In_The_Correct_Order()
    {
        // Arrange
        var handler = new InspectableHandler<SimpleMessage>();
        var outerMiddleware = new OuterTestMiddleware();
        var innerMiddleware = new InnerTestMiddleware();
        var afterMiddleware = new AfterTestMiddleware();

        var services = GivenJustSaying()
            .ConfigureJustSaying((builder) =>
                builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                    c => c.WithMiddlewareConfiguration(m =>
                    {
                        m.Use(outerMiddleware);
                        m.Use(innerMiddleware);
                        m.UseDefaults<SimpleMessage>(handler.GetType());
                        m.Use(afterMiddleware);
                    })))
            .AddJustSayingHandlers(new[] { handler });

        string json = "";
        await WhenAsync(
            services,
            (_, listener, _, _) =>
            {
                dynamic interrogation = listener.Interrogate();
                dynamic middlewares = interrogation.Data.Middleware;

                json = JsonConvert.SerializeObject(middlewares, Formatting.Indented)
                    .Replace(UniqueName, "TestQueueName");

                return Task.CompletedTask;
            });

        json.ShouldMatchApproved(c => c.SubFolder($"Approvals"));
    }
}