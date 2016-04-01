using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        Task AwaitAtLeastOneTaskToComplete();
        void ProcessMessage(Action action);

        int FreeTasks { get; }
    }
}