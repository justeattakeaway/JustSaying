using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

public class MessageContextAccessorTests
{
    [Fact]
    public void ContextIsNullByDefault()
    {
        var accessor = MakeAccessor();

        Assert.Null(accessor.MessageContext);
    }

    [Fact]
    public void CanStoreAndRetrieveContext()
    {
        var data = MakeUniqueMessageContext();
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        var readData = accessor.MessageContext;

        AssertSame(data, readData);
    }

    [Fact]
    public async Task CanStoreAndRetrieveAsync()
    {
        var data = MakeUniqueMessageContext();
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        await Task.Delay(250);

        AssertSame(data, accessor.MessageContext);
    }

    [Fact]
    public async Task DifferentThreadsHaveDifferentContexts()
    {
        var data1 = MakeUniqueMessageContext();
        var data2 = MakeUniqueMessageContext();

        var t1 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data1));
        var t2 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data2));

        await Task.WhenAll(t1, t2);
    }

    [Fact]
    public async Task MultiThreads()
    {
        var tasks = new List<Task>();

        for (int i = 0; i < 10; i++)
        {
            var data = MakeUniqueMessageContext();
            var task = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data));
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    [Fact]
    public async Task ThreadContextDoesNotEscape()
    {
        var data1 = MakeUniqueMessageContext();

        var t1 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data1));

        var accessor = MakeAccessor();
        Assert.Null(accessor.MessageContext);

        await t1;

        Assert.Null(accessor.MessageContext);
    }

    private static async Task ThreadLocalDataRemainsTheSame(MessageContext data)
    {
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(100 + i)
                .ConfigureAwait(false);

            AssertSame(data, accessor.MessageContext);

            accessor.MessageContext = data;
        }
    }

    private static void AssertSame(MessageContext expected, MessageContext actual)
    {
        Assert.NotNull(expected);
        Assert.NotNull(actual);

        Assert.Equal(expected, actual);
        Assert.Equal(expected.Message, actual.Message);
        Assert.Equal(expected.Message.Body, actual.Message.Body);
        Assert.Equal(expected.QueueUri, actual.QueueUri);
    }

    private static MessageContext MakeUniqueMessageContext()
    {
        var uniqueness = Guid.NewGuid().ToString();
        var queueUri = new Uri("http://test.com/" + uniqueness);

        var sqsMessage = new Message
        {
            Body = "test message " + uniqueness
        };

        return new MessageContext(sqsMessage, queueUri, new MessageAttributes());
    }

    private static MessageContextAccessor MakeAccessor()
    {
        return new MessageContextAccessor();
    }
}
