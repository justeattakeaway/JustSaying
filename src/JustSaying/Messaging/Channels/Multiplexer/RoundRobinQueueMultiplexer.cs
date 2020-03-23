using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.Channels.Context;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.Multiplexer
{
    internal sealed class RoundRobinQueueMultiplexer : IMultiplexer, IDisposable
    {
        private readonly ILogger<RoundRobinQueueMultiplexer> _logger;

        private readonly IList<ChannelReader<IQueueMessageContext>> _readers;
        private readonly Channel<IQueueMessageContext> _targetChannel;
        private readonly int _channelCapacity;

        private readonly SemaphoreSlim _readersLock = new SemaphoreSlim(1, 1);
        private readonly object _startLock = new object();

        private bool _started = false;
        private CancellationToken _stoppingToken;
        private Task _completion;

        public RoundRobinQueueMultiplexer(
            int channelCapacity,
            ILogger<RoundRobinQueueMultiplexer> logger)
        {
            _readers = new List<ChannelReader<IQueueMessageContext>>();
            _logger = logger;

            _channelCapacity = channelCapacity;
            _targetChannel = Channel.CreateBounded<IQueueMessageContext>(_channelCapacity);
        }

        public void ReadFrom(ChannelReader<IQueueMessageContext> reader)
        {
            if (reader == null) throw new ArgumentNullException(nameof(reader));

            _readersLock.Wait(_stoppingToken);

            try
            {
                _readers.Add(reader);
            }
            finally
            {
                _readersLock.Release();
            }

            async Task OnReaderCompletion()
            {
                await reader.Completion.ConfigureAwait(false);
                RemoveReader(reader);
            }

            _ = OnReaderCompletion();
        }

        private void RemoveReader(ChannelReader<IQueueMessageContext> reader)
        {
            _logger.LogInformation("Received notification to remove reader from multiplexer inputs");

            _readersLock.Wait(_stoppingToken);

            try
            {
                _readers.Remove(reader);
            }
            finally
            {
                _readersLock.Release();
            }
        }

        public Task Run(CancellationToken stoppingToken)
        {
            if (_started) return _completion;

            lock (_startLock)
            {
                if (_started) return _completion;

                _stoppingToken = stoppingToken;
                _completion = RunImpl();
                _started = true;

                return _completion;
            }
        }

        private async Task RunImpl()
        {
            await Task.Yield();

            _logger.LogInformation(
                "Starting up channel multiplexer with a queue capacity of {Capacity}",
                _channelCapacity);

            ChannelWriter<IQueueMessageContext> writer = _targetChannel.Writer;
            while (true)
            {
                await _readersLock.WaitAsync(_stoppingToken).ConfigureAwait(false);

                _stoppingToken.ThrowIfCancellationRequested();

                try
                {
                    if (_readers.Count < 1)
                    {
                        _logger.LogInformation("All writers have completed, terminating multiplexer");
                        writer.Complete();
                        break;
                    }

                    foreach (ChannelReader<IQueueMessageContext> reader in _readers)
                    {
                        if (reader.TryRead(out IQueueMessageContext message))
                        {
                            await writer.WriteAsync(message, _stoppingToken);
                        }
                    }
                }
                finally
                {
                    _readersLock.Release();
                }
            }
        }

        public async IAsyncEnumerable<IQueueMessageContext> GetMessagesAsync()
        {
            if (!_started)
            {
                throw new InvalidOperationException(
                    "Multiplexer must be started before listening to messages");
            }

            while (true)
            {
                bool couldWait = await _targetChannel.Reader.WaitToReadAsync(_stoppingToken);
                if (!couldWait) break;

                _stoppingToken.ThrowIfCancellationRequested();

                while (_targetChannel.Reader.TryRead(out IQueueMessageContext message))
                {
                    yield return message;
                }
            }
        }

        public void Dispose()
        {
            _readersLock.Dispose();
        }
    }
}
