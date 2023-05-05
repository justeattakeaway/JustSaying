using Amazon.SQS.Model;
using JustSaying.Messaging.Channels.Receive;
using JustSaying.Messaging.Middleware;
using JustSaying.Messaging.Middleware.Receive;
using JustSaying.TestingFramework;
using JustSaying.UnitTests.Messaging.Channels.SubscriptionGroupTests;
using Microsoft.Extensions.Logging;

namespace JustSaying.UnitTests.Messaging.Channels.MessageReceiveBufferTests;

public class WhenReceivingShouldStop
{
    private class TestMessage : Message { }

    private readonly ITestOutputHelper _testOutputHelper;
    private int _callCount;
    private MessageReceiveController _messageReceiveController;
    private ILoggerFactory _loggerFactory;
    private TrackingLoggingMonitor _monitor;
    private MiddlewareBase<ReceiveMessagesContext, IList<Message>> _sqsMiddleware;
    private FakeSqsQueue _queue;

    public WhenReceivingShouldStop(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task No_Messages_Are_Processed()
    {
        var messagesRead = await RunMessageReceiveBuffer(1, 0);

        // Make sure that number makes sense
        messagesRead.ShouldBe(0);
        messagesRead.ShouldBeLessThanOrEqualTo(_callCount);
    }

    [Fact]
    public async Task Messages_Are_Processed_After_Stopping_And_Starting()
    {
        var messagesRead = await RunMessageReceiveBuffer(1, 1);

        // Make sure that number makes sense
        messagesRead.ShouldBeGreaterThan(0);
        messagesRead.ShouldBeLessThanOrEqualTo(_callCount);
    }

    [Fact]
    public async Task Multiple_Tasks_Messages_Processed_After_Stopping_And_Starting()
    {
        var numberOfMessagesWithOneTask = await RunMessageReceiveBuffer(1, 1);
        var numberOfMessagesWithTwoTasks = await RunMessageReceiveBuffer(2, 1);

        //Messages should be doubled (with some leeway) - e.g. if MessageReceiveController uses AutoResetEvent, only one thread allowed
        ((double)numberOfMessagesWithTwoTasks).ShouldBeGreaterThan(numberOfMessagesWithOneTask * 1.75);
    }

    [Fact]
    public async Task More_Messages_Processed_After_Stopping_And_Starting_Multiple_Times()
    {
        var numberOfMessagesWithoutStoppingAgain = await RunMessageReceiveBuffer(1, 1);
        var numberOfMessagesWithStoppingAndStartingAgain = await RunMessageReceiveBuffer(1, 2);

        //Messages should keep being received after the controller status changes, unlike CancellationToken
        numberOfMessagesWithStoppingAndStartingAgain.ShouldBeGreaterThan(numberOfMessagesWithoutStoppingAgain);
    }

    private MessageReceiveBuffer CreateMessageReceiveBuffer()
    {
        _loggerFactory = _testOutputHelper.ToLoggerFactory();

        _sqsMiddleware =
            new DelegateMiddleware<ReceiveMessagesContext, IList<Message>>();

        var messages = new List<Message> { new TestMessage() };
        _queue = new FakeSqsQueue(ct =>
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(messages.AsEnumerable());
        });

        _messageReceiveController = new MessageReceiveController();

        _monitor = new TrackingLoggingMonitor(
            _loggerFactory.CreateLogger<TrackingLoggingMonitor>());

        var messageReceiveBuffer = new MessageReceiveBuffer(
            10,
            10,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(1),
            _queue,
            _sqsMiddleware,
            _messageReceiveController,
            _monitor,
            _loggerFactory.CreateLogger<IMessageReceiveBuffer>());

        return messageReceiveBuffer;
    }

    private async Task<int> Messages(MessageReceiveBuffer messageReceiveBuffer)
    {
        int messagesProcessed = 0;

        while (true)
        {
            var couldRead = await messageReceiveBuffer.Reader.WaitToReadAsync();
            if (!couldRead) break;

            while (messageReceiveBuffer.Reader.TryRead(out _))
            {
                messagesProcessed++;
            }
        }

        return messagesProcessed;
    }

    private async Task<int> RunMessageReceiveBuffer(int numberOfTasks, int startReceivingTimes)
    {
        var messageReceiveBuffer = CreateMessageReceiveBuffer();

        // Signal stop receiving messages
        _messageReceiveController.Stop();

        using var cts = new CancellationTokenSource();

        for (var i = 0; i < numberOfTasks; i++)
        {
            _ = messageReceiveBuffer.RunAsync(cts.Token);
        }

        var readTask = Messages(messageReceiveBuffer);

        if (startReceivingTimes == 1)
        {
            await MessageReceiveControllerStartWithDelays();
        }
        else if(startReceivingTimes > 1)
        {
            for (int i = 0; i < startReceivingTimes; i++)
            {
                await MessageReceiveControllerStartWithDelays();

                _messageReceiveController.Stop();
            }
        }
        else
        {
            // Check if we can start receiving for a while
            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        // Cancel token
        cts.Cancel();

        // Ensure buffer completes
        await messageReceiveBuffer.Reader.Completion;

        // Get the number of messages we read
        var messagesRead = await readTask;

        return messagesRead;
    }

    private async Task MessageReceiveControllerStartWithDelays()
    {
        // Check if we can start receiving for a while
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Signal start receiving messages
        _messageReceiveController.Start();

        // Read messages for a while
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
}
