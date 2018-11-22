using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class MessagePublisherExtensions
    {
        public static Task PublishAsync(this IMessagePublisher publisher, Message message)
        {
            return publisher.PublishAsync(message, CancellationToken.None);
        }

        public static async Task PublishAsync(this IMessagePublisher publisher,
            Message message, CancellationToken cancellationToken)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            await publisher.PublishAsync(new PublishEnvelope(message), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
