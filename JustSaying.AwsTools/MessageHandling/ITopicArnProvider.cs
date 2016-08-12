namespace JustSaying.AwsTools.MessageHandling
{
    public interface ITopicArnProvider
    {
        bool ArnExists();
        string GetArn();
    }
}
