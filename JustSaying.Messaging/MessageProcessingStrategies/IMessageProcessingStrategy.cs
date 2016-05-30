using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        /// <summary>
        /// The maximum number of tasks that will be used to handle messages at any one time
        /// </summary>
        int MaxConcurrentMessageHandlers { get; }

        /// <summary>
        /// The number of tasks that are free to handle messages right now,
        /// i.e. MaxConcurrentMessageHandlers - (the number of currently running tasks)
        /// Always in the range 0 >= x >= MaxConcurrentMessageHandlers
        /// </summary>
        int AvailableMessageHandlers { get; }

        /// <summary>
        /// Start processing a message. 
        /// </summary>
        /// <param name="action"></param>
        void ProcessMessage(Func<Task> action);

        /// <summary>
        /// after awaiting this, AvailableMessageHandlers should be > 0,
        /// i.e. you are in a position to add another one by calling ProcessMessage
        /// </summary>
        /// <returns></returns>
        Task AwaitAtLeastOneTaskToComplete();

    }
}