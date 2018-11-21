using System.Threading;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(PublishEnvelope message, CancellationToken cancellationToken);
    }

    public static class MessagePublisherHelpers
    {
        public static async Task PublishAsync(this IMessagePublisher publisher, Message message)
        {
            await publisher.PublishAsync(new PublishEnvelope(message), CancellationToken.None)
                .ConfigureAwait(false);
        }

        public static async Task PublishAsync(this IMessagePublisher publisher,
            Message message, CancellationToken cancellationToken)
        {
            await publisher.PublishAsync(new PublishEnvelope(message), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
