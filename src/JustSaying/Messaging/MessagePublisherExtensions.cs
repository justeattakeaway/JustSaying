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

        public static Task PublishAsync(this IMessagePublisher publisher,
            Message message, PublishMetadata metadata)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            return publisher.PublishAsync(message, metadata, CancellationToken.None);
        }

        public static Task PublishAsync(this IMessagePublisher publisher,
            Message message, CancellationToken cancellationToken)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            return publisher.PublishAsync(message, null, cancellationToken);
        }
    }
}
