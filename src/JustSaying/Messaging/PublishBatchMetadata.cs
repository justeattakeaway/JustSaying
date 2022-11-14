namespace JustSaying.Messaging;

public class PublishBatchMetadata : PublishMetadata
{
    public int BatchSize { get; set; } = 10;
}
