using System.Threading.Channels;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Multiplexer;
using JustSaying.TestingFramework;

namespace JustSaying.UnitTests.Messaging.Channels.MultiplexerTests;

public class MergingMultiplexerTests
{
    private readonly ITestOutputHelper _outputHelper;
    private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromSeconds(1);

    public MergingMultiplexerTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Starting_Twice_Returns_Same_Task()
    {
        // Arrange
        using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

        var cts = new CancellationTokenSource();

        // Act
        Task completed1 = multiplexer.RunAsync(cts.Token);
        Task completed2 = multiplexer.RunAsync(cts.Token);

        // Assert
        Assert.Equal(completed1, completed2);

        cts.Cancel();
        try
        {
            await completed1;
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
    }

    [Fact]
    public void Cannot_Add_Invalid_Reader()
    {
        // Arrange
        using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => multiplexer.ReadFrom(null));
    }

    [Fact]
    public async Task Cannot_Get_Messages_Before_Started()
    {
        // Arrange
        using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

        // Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => ReadAllMessages(multiplexer));
    }

    [Fact]
    public async Task When_Reader_Does_Not_Complete_Readers_Not_Completed()
    {
        // Arrange
        using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

        var cts = new CancellationTokenSource();

        var channel1 = Channel.CreateBounded<IQueueMessageContext>(10);
        var channel2 = Channel.CreateBounded<IQueueMessageContext>(10);
        multiplexer.ReadFrom(channel1);
        multiplexer.ReadFrom(channel2);

        // Act
        await multiplexer.RunAsync(cts.Token);
        var multiplexerRunTask = ReadAllMessages(multiplexer);

        channel1.Writer.Complete();

        // Assert
        var delay = Task.Delay(TimeoutPeriod);
        var completedTask = await Task.WhenAny(multiplexerRunTask, delay);
        Assert.Equal(delay, completedTask);

        cts.Cancel();
    }

    [Fact]
    public async Task When_Reader_Completes_When_All_Readers_Completed()
    {
        // Arrange
        using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

        var cts = new CancellationTokenSource();

        var channel1 = Channel.CreateBounded<IQueueMessageContext>(10);
        var channel2 = Channel.CreateBounded<IQueueMessageContext>(10);
        multiplexer.ReadFrom(channel1);
        multiplexer.ReadFrom(channel2);

        // Act
        await multiplexer.RunAsync(cts.Token);
        var multiplexerRunTask = ReadAllMessages(multiplexer);

        channel1.Writer.Complete();
        channel2.Writer.Complete();

        // Assert
        await Patiently.AssertThatAsync(_outputHelper, () => multiplexerRunTask.IsCompletedSuccessfully);
        cts.Cancel();
    }

    private static async Task ReadAllMessages(IMultiplexer multiplexer)
    {
        await foreach (var _ in multiplexer.GetMessagesAsync())
        {
        }
    }
}
