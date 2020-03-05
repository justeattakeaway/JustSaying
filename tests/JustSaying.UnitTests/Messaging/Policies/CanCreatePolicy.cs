using System;
using System.Threading.Tasks;
using JustSaying.Messaging.Policies;
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
            var noop = SqsPolicyBuilder.BuildAsync<int>();

            var result = await noop.RunAsync(async () =>
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
            var noop = SqsPolicyBuilder.BuildAsync<int>(
                next => new ErrorHandlingSqsPolicyAsync<int, InvalidOperationException>(next));

            var result = await noop.RunAsync(async () =>
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

            var policy = SqsPolicyBuilder.BuildAsync<int>(
                next => new PollySqsPolicyAsync<int>(next, pollyPolicy));

            var calledCount = 0;
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync(() =>
            {
                calledCount++;
                throw new CustomException();
            }));
            await Assert.ThrowsAsync<CustomException>(async () => await policy.RunAsync(() =>
            {
                calledCount++;
                throw new CustomException();
            }));
            await Assert.ThrowsAsync<BrokenCircuitException>(async () => await policy.RunAsync(() =>
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
