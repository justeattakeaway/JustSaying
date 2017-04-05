using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
        void Publish(Message message);
        Task PublishAsync(Message message);
    }
}
