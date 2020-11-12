using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenUsingMultipleMiddlewares : IntegrationTestBase
    {
        public WhenUsingMultipleMiddlewares(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        [AwsFact]
        public async Task Then_The_Middlewares_Are_Called()
        {
            var handler = new InspectableHandler<SimpleMessage>();

            var callRecord = new List<string>();

            void Before(string id) => callRecord.Add($"Before_{id}");
            void After(string id) => callRecord.Add($"After_{id}");

            var outerMiddleware = new TrackingMiddleware("outer", Before, After);
            var middleMiddleware = new TrackingMiddleware("middle", Before, After);
            var innerMiddleware = new TrackingMiddleware("inner", Before, After);

            var services = GivenJustSaying()
                .AddSingleton(outerMiddleware)
                .AddSingleton<IHandlerAsync<SimpleMessage>>(handler)
                .ConfigureJustSaying(builder =>
                    builder.WithLoopbackTopic<SimpleMessage>(UniqueName,
                        topic => topic.WithReadConfiguration(rc =>
                            rc.WithMiddlewareConfiguration(
                                pipe =>
                                {
                                    pipe.Use<TrackingMiddleware>(); // from DI
                                    pipe.Use(() => middleMiddleware); // provide a Func<MiddlewareBase<HandleMessageContext, bool>
                                    pipe.Use(innerMiddleware); // Existing instance
                                }))));

            await WhenAsync(services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    await listener.StartAsync(cancellationToken);
                    await publisher.StartAsync(cancellationToken);

                    // Act
                    await publisher.PublishAsync(new SimpleMessage(), cancellationToken);

                    await Patiently.AssertThatAsync(OutputHelper,
                        () => handler.ReceivedMessages.Any());
                });

            string.Join(Environment.NewLine, callRecord)
                .ShouldMatchApproved(c => c.SubFolder("Approvals"));

        }
    }
}
