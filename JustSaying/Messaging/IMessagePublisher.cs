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
        public static async Task PublishAsync<T>(this IMessagePublisher publisher, T message)
         where T: Message
        {
            await publisher.PublishAsync(new PublishEnvelope(message), CancellationToken.None)
                .ConfigureAwait(false);
        }

        public static async Task PublishAsync<T>(this IMessagePublisher publisher,
            T message, CancellationToken cancellationToken)
            where T : Message
        {
            await publisher.PublishAsync(new PublishEnvelope(message), cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
