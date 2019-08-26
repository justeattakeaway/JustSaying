using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent.Subscribing
{
    public class WhenHandlerIsDeclaredAsExactlyOnce : IntegrationTestBase
    {
        public WhenHandlerIsDeclaredAsExactlyOnce(ITestOutputHelper outputHelper)
            : base(outputHelper)
        {
        }

        [AwsFact]
        public async Task Then_The_Handler_Only_Receives_The_Message_Once()
        {
            // Arrange
            var handler = new ExactlyOnceHandlerWithTimeout();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Services((config) => config.WithMessageLock(() => new MessageLockStore())))
                .AddJustSayingHandlers(new[] { handler });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    listener.Start(cancellationToken);

                    var message = new SimpleMessage();

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await publisher.PublishAsync(message, cancellationToken);
                    await Task.Delay(10.Seconds());

                    // Assert
                    handler.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
                });
        }

        private sealed class MessageLockStore : IMessageLockAsync
        {
            private readonly ConcurrentDictionary<string, int> _store = new ConcurrentDictionary<string, int>();

            public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
            {
                var canAquire = !_store.TryGetValue(key, out int _);

                if (canAquire)
                {
                    _store.AddOrUpdate(key, 1, (_, i) => i + 1);
                }

                var response = new MessageLockResponse { DoIHaveExclusiveLock = canAquire };

                return Task.FromResult(response);
            }

            public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
                => TryAquireLockPermanentlyAsync(key);

            public Task ReleaseLockAsync(string key)
            {
                _store.TryRemove(key, out int _);
                return Task.CompletedTask;
            }
        }
    }
}
