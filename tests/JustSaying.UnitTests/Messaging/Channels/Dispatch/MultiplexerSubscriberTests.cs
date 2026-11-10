using System.Threading.Channels;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Dispatch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.UnitTests.Messaging.Channels.Dispatch;

public class MultiplexerSubscriberTests
{
    [Test]
    public async Task A_Message_That_Fails_To_Dispatch_Does_Not_Stop_The_Subscriber()
    {
        // Arrange
        var dispatched = new List<string>();
        var dispatcher = new ThrowingDispatcher("poison", dispatched);
        var logger = new FakeLogger<MultiplexerSubscriber>();
        var subscriber = new MultiplexerSubscriber(dispatcher, "subscriber-1", logger);

        var channel = Channel.CreateUnbounded<IQueueMessageContext>();
        await channel.Writer.WriteAsync(CreateContext("first"));
        await channel.Writer.WriteAsync(CreateContext("poison"));
        await channel.Writer.WriteAsync(CreateContext("last"));
        channel.Writer.Complete();

        subscriber.Subscribe(channel.Reader.ReadAllAsync());

        // Act
        await subscriber.RunAsync(CancellationToken.None);

        // Assert
        dispatched.ShouldBe(["first", "poison", "last"]);

        var error = logger.Collector.GetSnapshot().Where(r => r.Level >= LogLevel.Warning).ShouldHaveSingleItem();
        error.Level.ShouldBe(LogLevel.Error);
        error.Exception.ShouldBeOfType<InvalidOperationException>();
        error.Message.ShouldContain("'poison'");
    }

    [Test]
    public async Task Cancellation_Still_Stops_The_Subscriber()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var dispatcher = Substitute.For<IMessageDispatcher>();
        dispatcher.DispatchMessageAsync(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                cts.Cancel();
                throw new OperationCanceledException(cts.Token);
            });

        var subscriber = new MultiplexerSubscriber(dispatcher, "subscriber-1", new FakeLogger<MultiplexerSubscriber>());

        var channel = Channel.CreateUnbounded<IQueueMessageContext>();
        await channel.Writer.WriteAsync(CreateContext("first"));
        await channel.Writer.WriteAsync(CreateContext("second"));

        subscriber.Subscribe(channel.Reader.ReadAllAsync());

        // Act and Assert
        await Should.ThrowAsync<OperationCanceledException>(() => subscriber.RunAsync(cts.Token));
        await dispatcher.Received(1).DispatchMessageAsync(Arg.Any<IQueueMessageContext>(), Arg.Any<CancellationToken>());
    }

    private static IQueueMessageContext CreateContext(string messageId)
    {
        var context = Substitute.For<IQueueMessageContext>();
        context.Message.Returns(new SQSMessage { MessageId = messageId });
        context.QueueName.Returns("test-queue");
        return context;
    }

    private sealed class ThrowingDispatcher(string poisonMessageId, List<string> dispatched) : IMessageDispatcher
    {
        public Task DispatchMessageAsync(IQueueMessageContext messageContext, CancellationToken cancellationToken)
        {
            dispatched.Add(messageContext.Message.MessageId);

            if (messageContext.Message.MessageId == poisonMessageId)
            {
                throw new InvalidOperationException("Dispatch failed.");
            }

            return Task.CompletedTask;
        }
    }
}
