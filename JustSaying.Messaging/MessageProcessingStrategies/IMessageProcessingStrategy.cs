using System;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageProcessingStrategy
    {
        Task BeforeGettingMoreMessages();
        void ProcessMessage(Action action);

        int MaxBatchSize { get; }
    }
}