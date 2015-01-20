namespace JustSaying.AwsTools
{
    public interface IPublisher
    {
        void Publish(string subject, string message);
        void Configure();
    }
}