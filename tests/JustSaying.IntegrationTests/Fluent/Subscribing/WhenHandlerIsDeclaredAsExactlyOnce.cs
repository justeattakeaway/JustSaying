using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
            var messageLock = new MessageLockStore();
            var handler = new ExactlyOnceHandlerNoTimeout();

            var services = GivenJustSaying()
                .ConfigureJustSaying((builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName))
                .ConfigureJustSaying((builder) => builder.Services((config) => config.WithMessageLock(() => messageLock)))
                .AddJustSayingHandlers(new[] { handler });

            await WhenAsync(
                services,
                async (publisher, listener, serviceProvider, cancellationToken) =>
                {
                    _ = listener.StartAsync(cancellationToken);

                    var message = new SimpleMessage();

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await publisher.PublishAsync(message, cancellationToken);
                    await Task.Delay(5.Seconds());

                    // Assert
                    handler.NumberOfTimesIHaveBeenCalledForMessage(message.UniqueKey()).ShouldBe(1);
                });
        }

        private sealed class MessageLockStore : IMessageLockAsync
        {
            private readonly ConcurrentDictionary<string, int> _store = new ConcurrentDictionary<string, int>();

            public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
            {
                // Only the first attempt to access the value for the key can acquire the lock
                int newValue = _store.AddOrUpdate(key, 0, (_, i) => i + 1);

                var response = new MessageLockResponse
                {
                    DoIHaveExclusiveLock = newValue == 0,
                    IsMessagePermanentlyLocked = newValue == int.MinValue,
                };

                return Task.FromResult(response);
            }

            public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
            {
                _store.AddOrUpdate(key, int.MinValue, (_, i) => int.MinValue);

                var response = new MessageLockResponse
                {
                    DoIHaveExclusiveLock = true,
                    IsMessagePermanentlyLocked = true,
                };

                return Task.FromResult(response);
            }

            public Task ReleaseLockAsync(string key)
            {
                _store.Remove(key, out var _);
                return Task.CompletedTask;
            }
        }
    }
}
