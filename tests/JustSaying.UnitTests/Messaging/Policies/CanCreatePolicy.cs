using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging.Middleware;
using JustSaying.UnitTests.Messaging.Policies.ExamplePolicies;
using Polly;
using Polly.CircuitBreaker;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.Messaging.Policies
{
    public class CanCreatePolicy
    {
        [Fact]
        public async Task PolicyBuilder_Async()
        {
            var called = false;
            var noop = MiddlewareBuilder.BuildAsync<string, int>();

            var result = await noop.RunAsync("context", async ct =>
            {
                called = true;
                await Task.Delay(5, ct);
                return 1;
            }, CancellationToken.None);

            called.ShouldBeTrue();
            result.ShouldBe(1);
        }

        [Fact]
        public async Task MiddlewareBuilder_Error_Handler_Async()
        {
            var called = false;
            var noop = MiddlewareBuilder.BuildAsync(
                new ErrorHandlingMiddleware<string, int, InvalidOperationException>());

            var result = await noop.RunAsync("context", async ct =>
            {
                called = true;
                await Task.Delay(5, ct);
                throw new InvalidOperationException();
            }, CancellationToken.None);

            called.ShouldBeTrue();
            result.ShouldBe(0);
        }

        [Fact]
        public async Task Can_Integrate_With_Polly_Policies_Async()
        {
            var pollyPolicy = Policy
                .Handle<CustomException>()
                .CircuitBreakerAsync(
                    exceptionsAllowedBeforeBreaking: 2,
                    durationOfBreak: TimeSpan.FromSeconds(1));

            var policy = MiddlewareBuilder.BuildAsync(
                new PollyMiddleware<string, int>(pollyPolicy));

            var calledCount = 0;
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync("context", ct =>
            {
                calledCount++;
                throw new CustomException();
            }, CancellationToken.None));
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync("context", ct =>
            {
                calledCount++;
                throw new CustomException();
            }, CancellationToken.None));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () => await policy.RunAsync("context", ct =>
            {
                calledCount++;
                throw new CustomException();
            }, CancellationToken.None));

            calledCount.ShouldBe(2);
        }

        public class CustomException : Exception
        {
            public CustomException(string message) : base(message)
            {
            }

            public CustomException(string message, Exception innerException) : base(message, innerException)
            {
            }

            public CustomException()
            {
            }
        }
    }
}
