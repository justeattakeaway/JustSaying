using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels
{
    internal interface IMultiplexer
    {
        Task Start();
        void ReadFrom(ChannelReader<IQueueMessageContext> reader);
        IAsyncEnumerable<IQueueMessageContext> Messages();
    }

    public class RoundRobinQueueMultiplexer : IMultiplexer, IDisposable
    {
        readonly IList<ChannelReader<IQueueMessageContext>> _readers;
        readonly Channel<IQueueMessageContext> _targetChannel;

        readonly SemaphoreSlim _syncRoot = new SemaphoreSlim(1, 1);
        readonly ILogger<RoundRobinQueueMultiplexer> _logger;

        public RoundRobinQueueMultiplexer(ILoggerFactory loggerFactory)
        {
            _readers = new List<ChannelReader<IQueueMessageContext>>();
            _logger = loggerFactory.CreateLogger<RoundRobinQueueMultiplexer>();

            _targetChannel = Channel.CreateBounded<IQueueMessageContext>(_readers.Count * 10);
        }

        public void ReadFrom(ChannelReader<IQueueMessageContext> reader)
        {
            _syncRoot.Wait();
            _readers.Add(reader);
            _syncRoot.Release();

            reader.Completion.ContinueWith(c => RemoveReader(reader));
        }

        private void RemoveReader(ChannelReader<IQueueMessageContext> reader)
        {
            _logger.LogInformation("Received notification to remove reader from multiplexer inputs");

            _syncRoot.Wait();
            _readers.Remove(reader);
            _syncRoot.Release();
        }

        public async Task Start()
        {
            var writer = _targetChannel.Writer;
            while (true)
            {
                await _syncRoot.WaitAsync().ConfigureAwait(false);

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

                _syncRoot.Release();
            }
        }

        public async IAsyncEnumerable<IQueueMessageContext> Messages()
        {
            while(await _targetChannel.Reader.WaitToReadAsync())
            {
                if (_targetChannel.Reader.TryRead(out var message))
                    yield return message;
            }
        }

        public void Dispose()
        {
            _syncRoot?.Dispose();
        }
    }
}
