using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    public interface IHandlerWithMessageContext<in T>
    {
        Task<bool> HandleAsync(T message, MessageContext context);
    }
}
