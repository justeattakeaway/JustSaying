using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal interface IMultiplexer
    {
        Task Start();
        void ReadFrom(ChannelReader<IQueueMessageContext> reader);
        IAsyncEnumerable<IQueueMessageContext> Messages();
    }

    internal class RoundRobinQueueMultiplexer : IMultiplexer, IDisposable
    {
        private readonly IList<ChannelReader<IQueueMessageContext>> _readers;
        private Channel<IQueueMessageContext> _targetChannel;

        private readonly SemaphoreSlim _readersLock = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _startLock = new SemaphoreSlim(1, 1);
        readonly ILogger<RoundRobinQueueMultiplexer> _logger;

        private bool _started = false;
        private int _channelCapacity;

        public Task Completion { get; private set; }

        public RoundRobinQueueMultiplexer(ILoggerFactory loggerFactory)
        {
            _readers = new List<ChannelReader<IQueueMessageContext>>();
            _logger = loggerFactory.CreateLogger<RoundRobinQueueMultiplexer>();

            // TODO: make configurable
            _channelCapacity = 100;
            _targetChannel = Channel.CreateBounded<IQueueMessageContext>(_channelCapacity);
        }

        public void ReadFrom(ChannelReader<IQueueMessageContext> reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            _readersLock.Wait();
            _readers.Add(reader);
            _readersLock.Release();

            reader.Completion.ContinueWith(c => RemoveReader(reader), TaskScheduler.Default);
        }

        private void RemoveReader(ChannelReader<IQueueMessageContext> reader)
        {
            _logger.LogInformation("Received notification to remove reader from multiplexer inputs");

            _readersLock.Wait();
            _readers.Remove(reader);
            _readersLock.Release();
        }

        public async Task Start()
        {
            if (_started) return;

            await _startLock.WaitAsync().ConfigureAwait(false);

            if (_started) return;
            _started = true;

            try
            {
                Completion = Run();
            }
            finally
            {
                _startLock.Release();
            }
        }

        private async Task Run()
        {
            _logger.LogInformation("Starting up channel multiplexer with a queue capacity of {Capacity}",
                _channelCapacity);

            var writer = _targetChannel.Writer;
            while (true)
            {
                await _readersLock.WaitAsync().ConfigureAwait(false);

                if (!_readers.Any())
                {
                    _logger.LogInformation("All writers have completed, terminating multiplexer");
                    writer.Complete();
                    break;
                }

                foreach (var reader in _readers)
                {
                    if (reader.TryRead(out var message))
                    {
                        await writer.WriteAsync(message);
                    }
                }

                _readersLock.Release();
            }
        }

        public async IAsyncEnumerable<IQueueMessageContext> Messages()
        {
            await Start().ConfigureAwait(false);

            while (true)
            {
                using (_logger.TimedOperation("Waiting for messages to arrive to the multiplexer channel"))
                {
                    var couldWait = await _targetChannel.Reader.WaitToReadAsync();
                    if (!couldWait) break;
                }

                while (_targetChannel.Reader.TryRead(out var message))
                    yield return message;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _startLock.Dispose();
                _readersLock.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
