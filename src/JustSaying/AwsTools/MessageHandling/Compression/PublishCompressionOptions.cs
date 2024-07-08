namespace JustSaying.AwsTools.MessageHandling;

public sealed class PublishCompressionOptions
{
    public int MessageLengthThreshold { get; set; } = 256 * 1024;
    public string CompressionEncoding { get; set; }
}
