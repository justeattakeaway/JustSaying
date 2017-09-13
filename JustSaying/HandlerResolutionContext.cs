using JustSaying.Models;

namespace JustSaying
{
    public class HandlerResolutionContext
    {
        public HandlerResolutionContext(string queueName)
        {
            QueueName = queueName;
        }

        public string QueueName { get; }

        internal HandlerResolutionContextWithMessage WithMessage(Message message) => new HandlerResolutionContextWithMessage(QueueName, message);
    }
}