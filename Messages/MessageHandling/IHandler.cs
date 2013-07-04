namespace JustEat.Simples.NotificationStack.Messaging.MessageHandling
{
    public interface IHandles<in T>
    {
        bool Handle(T message);
    }
}
