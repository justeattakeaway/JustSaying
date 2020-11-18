using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Fluent;
using JustSaying.Messaging.Middleware.Handle;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Middleware
{
    public class HandlerMiddlewareBuilderTests
    {
        private readonly FakeServiceResolver _resolver;

        public HandlerMiddlewareBuilderTests()
        {
            _resolver = new FakeServiceResolver();
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

            var context = new HandleMessageContext(new SimpleMessage(),
                typeof(SimpleMessage),
                "a-fake-queue");

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
    }
}
