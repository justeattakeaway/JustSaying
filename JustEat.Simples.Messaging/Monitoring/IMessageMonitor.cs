namespace JustEat.Simples.NotificationStack.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void HandleException();
        void HandleTime(long handleTimeMs);
        void IssuePublishingMessage();
    }
}