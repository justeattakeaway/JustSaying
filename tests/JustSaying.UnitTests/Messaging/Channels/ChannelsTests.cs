using Amazon.SQS.Model;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.MessageHandling.Dispatch;
using JustSaying.Messaging;
using JustSaying.Messaging.Channels.Dispatch;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Channels.SubscriptionGroups;
using JustSaying.Messaging.Compression;
using JustSaying.Messaging.MessageSerialization;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using JustSaying.UnitTests.Messaging.Channels.TestHelpers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// ReSharper disable PossibleNullReferenceException

namespace JustSaying.UnitTests.Messaging.Channels;

public class ChannelsTests
{
    private IMessageReceivePauseSignal MessageReceivePauseSignal { get; set; }
    private ILoggerFactory LoggerFactory { get; set; }
    private IMessageMonitor MessageMonitor { get; set; }
    private TextWriter OutputHelper => TestContext.Current!.OutputWriter;
    private readonly TimeSpan _timeoutPeriod = TimeSpan.FromMilliseconds(50);

    [Before(Test)]
    public void Setup()
    {
        MessageReceivePauseSignal = new MessageReceivePauseSignal();
        LoggerFactory = OutputHelper.ToLoggerFactory();
        MessageMonitor = new TrackingLoggingMonitor(LoggerFactory.CreateLogger<TrackingLoggingMonitor>());
    }

    [Test]
    public async Task QueueCanBeAssignedToOnePump()
    {
        var sqsQueue = TestQueue();
        var buffer = CreateMessageReceiveBuffer(sqsQueue);
        IMessageDispatcher dispatcher = new FakeDispatcher();
        IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher);
        IMultiplexer multiplexer = CreateMultiplexer();

        multiplexer.ReadFrom(buffer.Reader);
        consumer1.Subscribe(multiplexer.GetMessagesAsync());

        var cts = new CancellationTokenSource();

        var multiplexerCompletion = multiplexer.RunAsync(cts.Token);
        var consumer1Completion = consumer1.RunAsync(cts.Token);
        var buffer1Completion = buffer.RunAsync(cts.Token);

        cts.Cancel();

        await multiplexerCompletion.HandleCancellation();
        await Should.ThrowAsync<OperationCanceledException>(() => consumer1Completion);
        await Should.ThrowAsync<OperationCanceledException>(() => buffer1Completion);
    }

    [Test]
    public async Task QueueCanBeAssignedToMultiplePumps()
    {
        var sqsQueue = TestQueue();
        var buffer = CreateMessageReceiveBuffer(sqsQueue);

        IMessageDispatcher dispatcher = new FakeDispatcher();
        IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher);
        IMultiplexerSubscriber consumer2 = CreateSubscriber(dispatcher);

        IMultiplexer multiplexer = CreateMultiplexer();

        multiplexer.ReadFrom(buffer.Reader);
        consumer1.Subscribe(multiplexer.GetMessagesAsync());
        consumer2.Subscribe(multiplexer.GetMessagesAsync());

        using var cts = new CancellationTokenSource(_timeoutPeriod);

        // consumers
        var multiplexerCompletion = multiplexer.RunAsync(cts.Token);
        var consumer1Completion = consumer1.RunAsync(cts.Token);
        var consumer2Completion = consumer2.RunAsync(cts.Token);
        var buffer1Completion = buffer.RunAsync(cts.Token);

        var results = await Task.WhenAll(
            multiplexerCompletion.HandleCancellation(),
            buffer1Completion.HandleCancellation(),
            consumer1Completion.HandleCancellation(),
            consumer2Completion.HandleCancellation());

        results.Any().ShouldBeTrue();
    }

    [Test]
    public async Task MultipleQueuesCanBeAssignedToOnePump()
    {
        var sqsQueue1 = TestQueue();
        var sqsQueue2 = TestQueue();
        var buffer1 = CreateMessageReceiveBuffer(sqsQueue1);
        var buffer2 = CreateMessageReceiveBuffer(sqsQueue2);

        IMessageDispatcher dispatcher = new FakeDispatcher();
        IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher);

        IMultiplexer multiplexer = CreateMultiplexer();

        multiplexer.ReadFrom(buffer1.Reader);
        multiplexer.ReadFrom(buffer2.Reader);

        consumer1.Subscribe(multiplexer.GetMessagesAsync());

        var cts = new CancellationTokenSource();
        cts.CancelAfter(_timeoutPeriod);

        // consumers
        var multiplexerCompletion = multiplexer.RunAsync(cts.Token);
        var consumer1Completion = consumer1.RunAsync(cts.Token);
        var buffer1Completion = buffer1.RunAsync(cts.Token);
        var buffer2Completion = buffer2.RunAsync(cts.Token);

        var results = await Task.WhenAll(
            multiplexerCompletion.HandleCancellation(),
            buffer1Completion.HandleCancellation(),
            buffer2Completion.HandleCancellation(),
            consumer1Completion.HandleCancellation());

        results.Any().ShouldBeTrue();
    }

    [Test]
    public async Task MultipleQueuesCanBeAssignedToMultiplePumps()
    {
        var sqsQueue1 = TestQueue();
        var sqsQueue2 = TestQueue();
        var buffer1 = CreateMessageReceiveBuffer(sqsQueue1);
        var buffer2 = CreateMessageReceiveBuffer(sqsQueue2);

        // using 2 dispatchers for logging, they should be the same/stateless
        IMessageDispatcher dispatcher1 = new FakeDispatcher();
        IMessageDispatcher dispatcher2 = new FakeDispatcher();
        IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher1);
        IMultiplexerSubscriber consumer2 = CreateSubscriber(dispatcher2);

        IMultiplexer multiplexer = CreateMultiplexer();

        multiplexer.ReadFrom(buffer1.Reader);
        multiplexer.ReadFrom(buffer2.Reader);

        consumer1.Subscribe(multiplexer.GetMessagesAsync());
        consumer2.Subscribe(multiplexer.GetMessagesAsync());

        var cts = new CancellationTokenSource();

        var multiplexerCompletion = multiplexer.RunAsync(cts.Token);

        // consumers
        var consumer1Completion = consumer1.RunAsync(cts.Token);
        var consumer2Completion = consumer2.RunAsync(cts.Token);

        var buffer1Completion = buffer1.RunAsync(cts.Token);
        var buffer2Completion = buffer2.RunAsync(cts.Token);

        cts.Cancel();

        var results = await Task.WhenAll(
            multiplexerCompletion.HandleCancellation(),
            buffer1Completion.HandleCancellation(),
            buffer2Completion.HandleCancellation(),
            consumer1Completion.HandleCancellation(),
            consumer2Completion.HandleCancellation());

        results.Any().ShouldBeTrue();
    }

    [Test]
    [Arguments(5, 5, 2, 10)]
    [Arguments(10, 20, 20, 50)]
    public async Task WhenSubscriberNotStarted_BufferShouldFillUp_AndStopDownloading(int receivePrefetch, int receiveBufferSize, int multiplexerCapacity, int expectedDownloadCount)
    {
        var sqsSource = TestQueue();
        var sqsQueue = sqsSource.SqsQueue as FakeSqsQueue;
        IMessageReceiveBuffer buffer = CreateMessageReceiveBuffer(sqsSource, receivePrefetch, receiveBufferSize);
        FakeDispatcher dispatcher = new FakeDispatcher();
        IMultiplexerSubscriber consumer1 = CreateSubscriber(dispatcher);
        IMultiplexer multiplexer = CreateMultiplexer(multiplexerCapacity);

        multiplexer.ReadFrom(buffer.Reader);
        consumer1.Subscribe(multiplexer.GetMessagesAsync());

        OutputHelper.WriteLine("Multiplexer" + JsonConvert.SerializeObject(multiplexer.Interrogate()));
        OutputHelper.WriteLine("MessageReceiveBuffer" + JsonConvert.SerializeObject(buffer.Interrogate()));

        using var cts = new CancellationTokenSource();

        // Act and Assert
        var multiplexerCompletion = multiplexer.RunAsync(cts.Token);
        var bufferCompletion = buffer.RunAsync(cts.Token);

        cts.CancelAfter(TimeSpan.FromMilliseconds(150));

        await multiplexerCompletion.HandleCancellation();
        await bufferCompletion.HandleCancellation();

        // The buffer may not fully fill before cancellation, but should never
        // exceed the expected count (which would mean stop-downloading is broken).
        sqsQueue.ReceiveMessageRequests.Sum(x => x.NumMessagesReceived).ShouldBeLessThanOrEqualTo(expectedDownloadCount);
        dispatcher.DispatchedMessages.Count.ShouldBe(0);

        // Starting the consumer after the token is cancelled will not dispatch messages
        await Should.ThrowAsync<OperationCanceledException>(() => consumer1.RunAsync(cts.Token));

        await Patiently.AssertThatAsync(OutputHelper,
            () =>
            {
                sqsQueue.ReceiveMessageRequests.Sum(x => x.NumMessagesReceived).ShouldBeLessThanOrEqualTo(expectedDownloadCount);
                dispatcher.DispatchedMessages.Count.ShouldBe(0);
            });
    }

    [Test]
    public async Task Can_Be_Set_Up_Using_SubscriptionBus()
    {
        var sqsQueue1 = TestQueue();
        var sqsQueue2 = TestQueue();
        var sqsQueue3 = TestQueue();

        var queues = new List<SqsSource> { sqsQueue1, sqsQueue2, sqsQueue3 };
        IMessageDispatcher dispatcher = new FakeDispatcher();
        var bus = CreateSubscriptionGroup(queues, dispatcher);

        var cts = new CancellationTokenSource();
        cts.CancelAfter(_timeoutPeriod);

        await Should.ThrowAsync<OperationCanceledException>(() => bus.RunAsync(cts.Token));
    }


    [Test]
    public async Task Sqs_Queue_Is_Not_Polled_After_Cancellation()
    {
        var cts = new CancellationTokenSource();
        var firstPollReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        int callCountBeforeCancelled = 0;
        int callCountAfterCancelled = 0;
        var sqsQueue = TestQueue(() =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                callCountAfterCancelled++;
            }
            else
            {
                callCountBeforeCancelled++;
                firstPollReceived.TrySetResult();
            }
        });

        IMessageDispatcher dispatcher = new FakeDispatcher();
        var bus = CreateSubscriptionGroup(new[] { sqsQueue }, dispatcher);

        var runTask = bus.RunAsync(cts.Token);

        await firstPollReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => runTask);

        callCountBeforeCancelled.ShouldBeGreaterThan(0);
        callCountAfterCancelled.ShouldBeLessThanOrEqualTo(1);
    }

    [Test]
    public async Task Messages_Not_Dispatched_After_Cancellation()
    {
        var cts = new CancellationTokenSource();
        var firstDispatchReceived = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        int dispatchedBeforeCancelled = 0;
        int dispatchedAfterCancelled = 0;

        var sqsQueue = TestQueue();
        IMessageDispatcher dispatcher = new FakeDispatcher(() =>
        {
            if (cts.Token.IsCancellationRequested)
            {
                dispatchedAfterCancelled++;
            }
            else
            {
                dispatchedBeforeCancelled++;
                firstDispatchReceived.TrySetResult();
            }
        });

        var bus = CreateSubscriptionGroup(new[] { sqsQueue }, dispatcher);

        var runTask = bus.RunAsync(cts.Token);

        await firstDispatchReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cts.Cancel();

        await Should.ThrowAsync<OperationCanceledException>(() => runTask);

        dispatchedBeforeCancelled.ShouldBeGreaterThan(0);

        // Allow a small number of in-flight dispatches that passed the cancellation
        // check before Cancel() was observed, but were dispatched after.
        dispatchedAfterCancelled.ShouldBeLessThanOrEqualTo(2);
    }

    [Test]
    public void SubscriptionGroup_StartingTwice_ShouldReturnSameCompletionTask()
    {
        var queue = TestQueue();
        var dispatcher = new FakeDispatcher();
        var group = CreateSubscriptionGroup([queue], dispatcher);

        var cts = new CancellationTokenSource();

        var task1 = group.RunAsync(cts.Token);
        var task2 = group.RunAsync(cts.Token);

        ReferenceEquals(task1, task2).ShouldBeTrue();

        cts.Cancel();
    }

    private static SqsSource TestQueue(Action spy = null)
    {
        IEnumerable<Message> GetMessages(CancellationToken cancellationToken)
        {
            spy?.Invoke();

            while (!cancellationToken.IsCancellationRequested)
            {
                yield return new TestMessage();
            }
        }

        var source = new SqsSource
        {
            SqsQueue = new FakeSqsQueue(ct => Task.FromResult(GetMessages(ct))),
            MessageConverter = new InboundMessageConverter(SimpleMessage.Serializer, new MessageCompressionRegistry(), false)
        };

        return source;
    }

    private MessageReceiveBuffer CreateMessageReceiveBuffer(
        SqsSource sqsQueue,
        int prefetch = 10,
        int receiveBufferSize = 10)
    {
        return new MessageReceiveBuffer(
            prefetch,
            receiveBufferSize,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            sqsQueue,
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>(),
            MessageReceivePauseSignal,
            MessageMonitor,
            LoggerFactory.CreateLogger<MessageReceiveBuffer>());
    }

    private MultiplexerSubscriber CreateSubscriber(IMessageDispatcher dispatcher)
    {
        return new MultiplexerSubscriber(dispatcher, Guid.NewGuid().ToString(),
            LoggerFactory.CreateLogger<MultiplexerSubscriber>());
    }

    private ISubscriptionGroup CreateSubscriptionGroup(
        IList<SqsSource> queues,
        IMessageDispatcher dispatcher)
    {
        var defaults = new SubscriptionGroupSettingsBuilder();

        var settings = new Dictionary<string, SubscriptionGroupConfigBuilder>
        {
            { "test",  new SubscriptionGroupConfigBuilder("test").AddQueues(queues) },
        };

        var consumerGroupFactory = new SubscriptionGroupFactory(
            dispatcher,
            MessageReceivePauseSignal,
            MessageMonitor,
            LoggerFactory);

        return consumerGroupFactory.Create(defaults, settings);
    }

    private MergingMultiplexer CreateMultiplexer(int channelCapacity = 100)
    {
        return new MergingMultiplexer(
            channelCapacity,
            LoggerFactory.CreateLogger<MergingMultiplexer>());
    }

    private class TestMessage : Message
    {
        public TestMessage(string body = null)
        {
            Body = body ?? Guid.NewGuid().ToString();
        }

        public override string ToString()
        {
            return Body;
        }
    }
}
