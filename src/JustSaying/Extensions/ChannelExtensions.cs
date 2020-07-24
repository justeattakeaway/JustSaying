using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace JustSaying.Extensions
{
    public static class ChannelExtensions
    {
        public static Task MergeAsync<T>(
            IEnumerable<ChannelReader<T>> inputs,
            ChannelWriter<T> output,
            CancellationToken stoppingToken)
        {
            if (inputs is null) throw new ArgumentNullException(nameof(inputs));
            if (output is null) throw new ArgumentNullException(nameof(output));

            return Task.Run(async () =>
                {
                    try
                    {
                        await Task.WhenAll(inputs.Select(input => RedirectAsync(input, output, stoppingToken))
                                .ToArray())
                            .ConfigureAwait(false);

                        output.Complete();
                    }
                    catch (Exception ex)
                    {
                        output.Complete(ex);
                        throw;
                    }
                },
                stoppingToken);
        }

        private static async Task RedirectAsync<T>(
            ChannelReader<T> input,
            ChannelWriter<T> output,
            CancellationToken stoppingToken)
        {
            await foreach (var item in ReadAllAsync(input, stoppingToken))
            {
                await output.WriteAsync(item, stoppingToken).ConfigureAwait(false);
            }
        }

        private static async IAsyncEnumerable<T> ReadAllAsync<T>(
            ChannelReader<T> reader,
            [EnumeratorCancellation] CancellationToken stoppingToken)
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
    }
}
