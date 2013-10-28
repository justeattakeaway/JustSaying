namespace JustEat.Simples.NotificationStack.Messaging.Monitoring
{
    public interface IMessageMonitor
    {
        void Handled();
        void HandleException();
        void HandleTime(long handTimeMs);
    }
}