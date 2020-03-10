using System;
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
            var noop = MiddlewareBuilder.BuildAsync<int, int>();

            var result = await noop.RunAsync(1, async () =>
            {
                called = true;
                await Task.Delay(5);
                return 1;
            });

            called.ShouldBeTrue();
            result.ShouldBe(1);
        }

        [Fact]
        public async Task PolicyBuilder_Error_Handler_Async()
        {
            var called = false;
            var noop = MiddlewareBuilder.BuildAsync<int, int>(
                next => new ErrorHandlingMiddleware<int, int, InvalidOperationException>(next));

            var result = await noop.RunAsync(1, async () =>
            {
                called = true;
                await Task.Delay(5);
                throw new InvalidOperationException();
            });

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

            var policy = MiddlewareBuilder.BuildAsync<int, int>(
                next => new PollyMiddleware<int, int>(next, pollyPolicy));

            var calledCount = 0;
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync(1, () =>
            {
                calledCount++;
                throw new CustomException();
            }));
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync(1, () =>
            {
                calledCount++;
                throw new CustomException();
            }));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () => await policy.RunAsync(1, () =>
            {
                calledCount++;
                throw new CustomException();
            }));

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
