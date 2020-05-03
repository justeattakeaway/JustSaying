using System.Threading;
using System.Threading.Tasks;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(T message, PublishMetadata metadata, CancellationToken cancellationToken) where T : class;
    }
}
