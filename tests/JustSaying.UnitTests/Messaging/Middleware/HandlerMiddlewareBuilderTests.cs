using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Middleware
{
    public class HandlerMiddlewareBuilderTests
    {
        private readonly InMemoryServiceResolver _resolver;

        public HandlerMiddlewareBuilderTests()
        {
            _resolver = new InMemoryServiceResolver();
        }

        [Fact]
        public async Task ThreeMiddlewares_ShouldExecuteInCorrectOrder()
        {
            var callRecord = new List<string>();

            void Before(string id) => callRecord.Add($"Before_{id}");
            void After(string id) => callRecord.Add($"After_{id}");

            var outer = new TrackingMiddleware("outer", Before, After);
            var middle = new TrackingMiddleware("middle", Before, After);
            var inner = new TrackingMiddleware("inner", Before, After);

            var middleware = new HandlerMiddlewareBuilder(_resolver, _resolver)
                .Configure(pipe =>
                {
                    pipe.Use(outer);
                    pipe.Use(middle);
                    pipe.Use(inner);
                }).Build();

            var context = TestHandleContexts.From<SimpleMessage>();

            await middleware.RunAsync(context,
                ct =>
                {
                    callRecord.Add("HandledMessage");
                    return Task.FromResult(true);
                },
                CancellationToken.None);

            var record = string.Join(Environment.NewLine, callRecord);

            record.ShouldMatchApproved(c => c.SubFolder("Approvals"));
        }

        [Fact]
        public async Task MiddlewareBuilder_WithoutDefaults_ShouldExecute()
        {
            var callRecord = new List<string>();

            void Before(string id) => callRecord.Add($"Before_{id}");
            void After(string id) => callRecord.Add($"After_{id}");

            var outer = new TrackingMiddleware("outer", Before, After);
            var inner = new TrackingMiddleware("inner", Before, After);

            var handler = new InspectableHandler<SimpleMessage>();

            var middlewareBuilder = new HandlerMiddlewareBuilder(_resolver, _resolver)
                .Configure(hmb =>
                    hmb.Use(outer)
                        .Use(inner)
                        .UseHandler(ctx => handler));

            var handlerMiddleware = middlewareBuilder.Build();

            var context = TestHandleContexts.From<SimpleMessage>();

            await handlerMiddleware.RunAsync(context, null, CancellationToken.None);

            callRecord.ShouldBe(new[] { "Before_outer", "Before_inner", "After_inner", "After_outer" });
            handler.ReceivedMessages.ShouldHaveSingleItem();
        }
    }
}
