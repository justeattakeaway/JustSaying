using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent.Publishing;

/// <summary>
/// Proves that publishing through a base-typed (or <see cref="object"/>-typed) reference routes and
/// serializes by the message's <em>runtime</em> type, not the compile-time generic argument. This
/// guards against re-introducing declared-type (<c>typeof(T)</c>) routing, which would send a
/// base-typed publish to a non-existent base-type publisher and trim derived properties off the wire.
/// </summary>
public class WhenPublishingABaseTypedMessage : IntegrationTestBase
{
    public abstract class Animal
    {
        public string Name { get; set; }
    }

    public sealed class Dog : Animal
    {
        public bool GoodBoy { get; set; }
    }

    public sealed class Cat : Animal
    {
        public int Lives { get; set; }
    }

    [Test]
    public async Task Then_A_Base_Typed_Publish_Routes_To_The_Runtime_Type()
    {
        // Arrange
        var completionSource = new TaskCompletionSource<Dog>(TaskCreationOptions.RunContinuationsAsynchronously);

        var handler = Substitute.For<IHandlerAsync<Dog>>();
        handler.Handle(Arg.Any<Dog>())
            .Returns(true)
            .AndDoes(call => completionSource.TrySetResult(call.Arg<Dog>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p => p.WithQueue<Dog>(o => o.WithQueueName(UniqueName)))
                .Subscriptions(s => s.ForQueue<Dog>(sub => sub.WithQueueName(UniqueName))))
            .AddSingleton(handler);

        await WhenAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act - publish via the base type; the static type at the call site is Animal,
                // but the runtime type is Dog and only a Dog publisher is registered.
                Animal animal = new Dog { Name = "Rex", GoodBoy = true };
                await publisher.PublishAsync(animal, cancellationToken);

                // Assert - it routed to the Dog publisher/handler, and the derived GoodBoy
                // property survived serialization (no declared-type trimming).
                var handled = await completionSource.Task.WaitAsync(cancellationToken);
                handled.Name.ShouldBe("Rex");
                handled.GoodBoy.ShouldBeTrue();
            });
    }

    [Test]
    public async Task Then_A_Heterogeneous_Base_Typed_Batch_Routes_Each_Message_To_Its_Runtime_Type()
    {
        // Arrange
        var dogHandled = new TaskCompletionSource<Dog>(TaskCreationOptions.RunContinuationsAsynchronously);
        var catHandled = new TaskCompletionSource<Cat>(TaskCreationOptions.RunContinuationsAsynchronously);

        var dogHandler = Substitute.For<IHandlerAsync<Dog>>();
        dogHandler.Handle(Arg.Any<Dog>())
            .Returns(true)
            .AndDoes(call => dogHandled.TrySetResult(call.Arg<Dog>()));

        var catHandler = Substitute.For<IHandlerAsync<Cat>>();
        catHandler.Handle(Arg.Any<Cat>())
            .Returns(true)
            .AndDoes(call => catHandled.TrySetResult(call.Arg<Cat>()));

        var services = GivenJustSaying()
            .ConfigureJustSaying(builder => builder
                .Publications(p =>
                {
                    p.WithQueue<Dog>(o => o.WithQueueName(UniqueName + "-dog"));
                    p.WithQueue<Cat>(o => o.WithQueueName(UniqueName + "-cat"));
                })
                .Subscriptions(s =>
                {
                    s.ForQueue<Dog>(sub => sub.WithQueueName(UniqueName + "-dog"));
                    s.ForQueue<Cat>(sub => sub.WithQueueName(UniqueName + "-cat"));
                }))
            .AddSingleton(dogHandler)
            .AddSingleton(catHandler);

        await WhenBatchAsync(
            services,
            async (publisher, listener, cancellationToken) =>
            {
                await listener.StartAsync(cancellationToken);
                await publisher.StartAsync(cancellationToken);

                // Act - a single batch of base-typed references holding two different runtime types.
                var batch = new List<Animal>
                {
                    new Dog { Name = "Rex", GoodBoy = true },
                    new Cat { Name = "Tom", Lives = 9 },
                };
                await publisher.PublishBatchAsync(batch, cancellationToken);

                // Assert - the batch was fanned out by runtime type to each concrete publisher/handler.
                var dog = await dogHandled.Task.WaitAsync(cancellationToken);
                dog.Name.ShouldBe("Rex");
                dog.GoodBoy.ShouldBeTrue();

                var cat = await catHandled.Task.WaitAsync(cancellationToken);
                cat.Name.ShouldBe("Tom");
                cat.Lives.ShouldBe(9);
            });
    }
}
