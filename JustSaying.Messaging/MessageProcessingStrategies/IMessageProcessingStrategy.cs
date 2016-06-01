using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        /// <summary>
        /// The maximum number of worker tasks that will be used to run messages handlers at any one time
        /// </summary>
        int MaxWorkers { get; }

        /// <summary>
        /// The number of worker tasks that are free to run messages handlers right now,
        /// i.e. MaxWorkers - (the number of currently running workers)
        /// Always in the range: 0 >= AvailableWorkers >= MaxWorkers
        /// </summary>
        int AvailableWorkers { get; }

        /// <summary>
        /// Start processing a message. 
        /// </summary>
        /// <param name="action"></param>
        void ProcessMessage(Func<Task> action);

        /// <summary>
        /// after awaiting this, AvailableWorkers should be > 0,
        /// i.e. you are in a position to add another one by calling ProcessMessage
        /// </summary>
        /// <returns></returns>
        Task AwaitAtLeastOneWorkerToComplete();

    }
}