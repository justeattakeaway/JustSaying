using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JustSaying.Extensions
{
    public static class ChannelExtensions
    {
        public static void Merge<T>(
            IEnumerable<ChannelReader<T>> inputs,
            ChannelWriter<T> output,
            CancellationToken stoppingToken)
        {
            async IAsyncEnumerable<T> ReadAllAsync(ChannelReader<T> reader)
            {
                if (reader == null) throw new ArgumentNullException(nameof(reader));

                while (await reader.WaitToReadAsync(stoppingToken).ConfigureAwait(false))
                {
                    while (reader.TryRead(out T item))
                    {
                        yield return item;
                    }
                }
            }

            Task.Run(async () =>
                {
                    async Task Redirect(ChannelReader<T> input)
                    {
                        await foreach (var item in ReadAllAsync(input))
                            await output.WriteAsync(item, stoppingToken).ConfigureAwait(false);
                    }

                    await Task.WhenAll(inputs.Select(Redirect).ToArray()).ConfigureAwait(false);
                    output.Complete();
                },
                stoppingToken);
        }
    }
}
