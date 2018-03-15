using System.Threading;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
#if AWS_SDK_HAS_SYNC
        void Publish(Message message);
#endif
        Task PublishAsync(Message message);

        Task PublishAsync(Message message, CancellationToken cancellationToken);
    }
}
