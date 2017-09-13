namespace JustSaying.Messaging
{
    public interface IMessageSubscriber
    {
        void StartListening();
        void StopListening();
        bool Listening { get; }
    }
}