namespace JustEat.Simples.NotificationStack.Messaging.MessageHandling
{
    public interface IHandler<in T>
    {
        bool Handle(T message);
    }
}