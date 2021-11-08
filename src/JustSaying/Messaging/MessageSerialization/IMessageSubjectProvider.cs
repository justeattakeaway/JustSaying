namespace JustSaying.Messaging.MessageSerialization
{
    public interface IMessageSubjectProvider
    {
        string GetSubjectForType(Type messageType);
    }
}
