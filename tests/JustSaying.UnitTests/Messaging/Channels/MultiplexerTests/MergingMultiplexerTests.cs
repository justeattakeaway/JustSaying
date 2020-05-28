using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Channels.Multiplexer;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.Messaging.Channels.MultiplexerTests
{
    public class MergingMultiplexerTests
    {
        private readonly ITestOutputHelper _outputHelper;
        private static readonly TimeSpan TimeoutPeriod = TimeSpan.FromMilliseconds(100);

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
            Task completed1 = multiplexer.Run(cts.Token);
            Task completed2 = multiplexer.Run(cts.Token);

            // Assert
            Assert.Equal(completed1, completed2);

            cts.Cancel();
            await completed1;
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
        public void Cannot_Get_Messages_Before_Started()
        {
            // Arrange
            using var multiplexer = new MergingMultiplexer(10, _outputHelper.ToLogger<MergingMultiplexer>());

            // Act and Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await ReadAllMessages(multiplexer));
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
            await multiplexer.Run(cts.Token);
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
            await multiplexer.Run(cts.Token);
            var multiplexerRunTask = ReadAllMessages(multiplexer);
            
            channel1.Writer.Complete();
            channel2.Writer.Complete();

            // Assert
            var delay = Task.Delay(TimeoutPeriod);
            var completedTask = await Task.WhenAny(multiplexerRunTask, delay);
            Assert.Equal(multiplexerRunTask, completedTask);

            cts.Cancel();
        }

        private static async Task ReadAllMessages(IMultiplexer multiplexer)
        {
            await foreach (var _ in multiplexer.GetMessagesAsync())
            {
            }
        }
    }
}
