using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(PublishEnvelope message, CancellationToken cancellationToken);
    }
}
