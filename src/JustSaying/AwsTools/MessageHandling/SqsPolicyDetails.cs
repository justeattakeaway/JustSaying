namespace JustSaying.AwsTools.MessageHandling;

internal class SqsPolicyDetails
{
    public string SourceArn { get; set; }
    public string QueueArn { get; set; }
    public Uri QueueUri { get; set; }
}