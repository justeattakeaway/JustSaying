using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        Task AwaitAtLeastOneTaskToComplete();
        void ProcessMessage(Func<Task> action);

        int FreeTasks { get; }
    }
}