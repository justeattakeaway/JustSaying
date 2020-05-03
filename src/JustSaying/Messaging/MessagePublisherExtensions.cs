using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MessagePublisherExtensions
    {
        public static Task PublishAsync<T>(this IMessagePublisher publisher, T message)
            where T : class
        {
            return publisher.PublishAsync(message, CancellationToken.None);
        }

        public static async Task PublishAsync<T>(this IMessagePublisher publisher,
            T message, PublishMetadata metadata)
            where T : class
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            await publisher.PublishAsync(message, metadata, CancellationToken.None)
                .ConfigureAwait(false);
        }

        public static async Task PublishAsync<T>(this IMessagePublisher publisher,
            T message, CancellationToken cancellationToken)
            where T : class
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            await publisher.PublishAsync(message, null, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
