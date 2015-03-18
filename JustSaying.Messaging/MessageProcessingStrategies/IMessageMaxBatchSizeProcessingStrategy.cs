namespace JustSaying.Messaging.MessageProcessingStrategies
{
    public interface IMessageMaxBatchSizeProcessingStrategy
    {
        int MaxBatchSize { get; }
    }
}