using System.Threading;
using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        Task PublishAsync(Message message);

        Task PublishAsync(Message message, CancellationToken cancellationToken);
    }
}
