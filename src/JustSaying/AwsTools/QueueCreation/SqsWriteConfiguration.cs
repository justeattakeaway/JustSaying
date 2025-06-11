using JustSaying.AwsTools.MessageHandling;

namespace JustSaying.AwsTools.QueueCreation;

public class SqsWriteConfiguration : SqsBasicConfiguration
{
    public PublishCompressionOptions CompressionOptions { get; set; }
}
