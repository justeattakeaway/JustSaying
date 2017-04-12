using System.Threading.Tasks;
using JustSaying.Models;

namespace JustSaying.Messaging
{
    public interface IMessagePublisher
    {
#if NET451
        void Publish(Message message);
#endif
        Task PublishAsync(Message message);
    }
}
