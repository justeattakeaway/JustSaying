using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.UnitTests.AwsTools.MessageHandling;

public class MessageContextAccessorTests
{
    [Test]
    public void ContextIsNullByDefault()
    {
        var accessor = MakeAccessor();

        accessor.MessageContext.ShouldBeNull();
    }

    [Test]
    public void CanStoreAndRetrieveContext()
    {
        var data = MakeUniqueMessageContext();
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        var readData = accessor.MessageContext;

        AssertSame(data, readData);
    }

    [Test]
    public async Task CanStoreAndRetrieveAsync()
    {
        var data = MakeUniqueMessageContext();
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        await Task.Delay(50);

        AssertSame(data, accessor.MessageContext);
    }

    [Test]
    public async Task DifferentThreadsHaveDifferentContexts()
    {
        var data1 = MakeUniqueMessageContext();
        var data2 = MakeUniqueMessageContext();

        var t1 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data1));
        var t2 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data2));

        await Task.WhenAll(t1, t2);
    }

    [Test]
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

    [Test]
    public async Task ThreadContextDoesNotEscape()
    {
        var data1 = MakeUniqueMessageContext();

        var t1 = Task.Run(async () => await ThreadLocalDataRemainsTheSame(data1));

        var accessor = MakeAccessor();
        accessor.MessageContext.ShouldBeNull();

        await t1;

        accessor.MessageContext.ShouldBeNull();
    }

    private static async Task ThreadLocalDataRemainsTheSame(MessageContext data)
    {
        var accessor = MakeAccessor();
        accessor.MessageContext = data;

        for (int i = 0; i < 5; i++)
        {
            await Task.Yield();

            AssertSame(data, accessor.MessageContext);

            accessor.MessageContext = data;
        }
    }

    private static void AssertSame(MessageContext expected, MessageContext actual)
    {
        expected.ShouldNotBeNull();
        actual.ShouldNotBeNull();

        actual.ShouldBe(expected);
        actual.Message.ShouldBe(expected.Message);
        actual.Message.Body.ShouldBe(expected.Message.Body);
        actual.QueueUri.ShouldBe(expected.QueueUri);
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
