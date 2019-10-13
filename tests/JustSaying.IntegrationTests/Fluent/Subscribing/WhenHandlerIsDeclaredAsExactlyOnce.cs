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
                    listener.Start(cancellationToken);

                    var message = new SimpleMessage()
                    {
                        Id = new Guid("4f598bf3-67a1-49a3-9d45-630fe1f9dab5")
                    };

                    // Act
                    await publisher.PublishAsync(message, cancellationToken);
                    await publisher.PublishAsync(message, cancellationToken);
                    await Task.Delay(5.Seconds());

                    // Assert
                    handler.NumberOfTimesIHaveBeenCalled().ShouldBe(1);
                });
        }

        private sealed class MessageLockStore : IMessageLockAsync
        {
            private readonly ConcurrentDictionary<string, int> _store = new ConcurrentDictionary<string, int>();

            public Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong)
            {
                // Only the first attempt to access the value for the key can acquire the lock
                int newValue = _store.AddOrUpdate(key, (_) => 0, (_, i) => i + 1);

                var response = new MessageLockResponse
                {
                    DoIHaveExclusiveLock = newValue == 0,
                    IsMessagePermanentlyLocked = newValue == int.MinValue,
                };

                return Task.FromResult(response);
            }

            public Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key)
            {
                _store.AddOrUpdate(key, (_) => int.MinValue, (_, i) => int.MinValue);

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
