using JustSaying.Models;

namespace JustSaying.Messaging.Channels.Context
{
    public sealed class HandleMessageContext
    {
        public Message Message { get; set; }
    }
}
