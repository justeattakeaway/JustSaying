using System.Threading.Channels;
using JustSaying.Extensions;
using JustSaying.Messaging.Channels.Context;
using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Multiplexer
{
    internal sealed class MergingMultiplexer : IMultiplexer, IDisposable
    {
        private readonly ILogger<MergingMultiplexer> _logger;

        private bool _started;
        private CancellationToken _stoppingToken;
        private Task _completion;

        private readonly IList<ChannelReader<IQueueMessageContext>> _readers;
        private readonly Channel<IQueueMessageContext> _targetChannel;
        private readonly int _channelCapacity;

        private readonly SemaphoreSlim _readersLock = new SemaphoreSlim(1, 1);
        private readonly object _startLock = new object();

        public MergingMultiplexer(
            int channelCapacity,
            ILogger<MergingMultiplexer> logger)
        {
            _readers = new List<ChannelReader<IQueueMessageContext>>();
            _logger = logger;

            _channelCapacity = channelCapacity;
            _targetChannel = Channel.CreateBounded<IQueueMessageContext>(_channelCapacity);
        }

        public Task RunAsync(CancellationToken stoppingToken)
        {
            if (!_started)
            {
                lock (_startLock)
                {
                    if (!_started)
                    {
                        _stoppingToken = stoppingToken;
                        _completion = RunImplAsync();
                        _started = true;
                    }
                }
            }

            return _completion;
        }

        private async Task RunImplAsync()
        {
            await Task.Yield();

            await _readersLock.WaitAsync(_stoppingToken).ConfigureAwait(false);

            try
            {
                _logger.LogDebug(
                    "Starting up channel multiplexer with a queue capacity of {Capacity}",
                    _channelCapacity);

                _ = ChannelExtensions.MergeAsync(_readers, _targetChannel.Writer, _stoppingToken);
            }
            finally
            {
                _readersLock.Release();
            }
        }

        public InterrogationResult Interrogate()
        {
            return new InterrogationResult(new
            {
                ChannelCapacity = _channelCapacity,
                ReaderCount = _readers.Count,
            });
        }

        public void ReadFrom(ChannelReader<IQueueMessageContext> reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            _readersLock.Wait(_stoppingToken);

            try
            {
                if (_started)
                    throw new InvalidOperationException("Cannot add readers once the multiplexer has started.");

                _readers.Add(reader);
            }
            finally
            {
                _readersLock.Release();
            }
        }

        public async IAsyncEnumerable<IQueueMessageContext> GetMessagesAsync()
        {
            if (!_started)
                throw new InvalidOperationException("Multiplexer must be started before listening to messages.");

            async IAsyncEnumerable<T> ReadAllAsync<T>(ChannelReader<T> reader)
            {
                while (await reader.WaitToReadAsync(_stoppingToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out T item))
                    {
                        yield return item;
                    }
                }
            }

            await foreach (IQueueMessageContext msg in ReadAllAsync(_targetChannel.Reader))
                yield return msg;
        }

        public void Dispose()
        {
            _completion?.Dispose();
            _readersLock?.Dispose();

            _completion = null;
        }
    }
}
