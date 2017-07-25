using JustSaying.Models;

namespace JustSaying
{
    public class HandlerResolutionContextWithMessage: HandlerResolutionContext
    {
        public HandlerResolutionContextWithMessage(string queueName, Message message): base(queueName)
        {
            Message = message;
        }

        public Message Message { get; }
    }
}