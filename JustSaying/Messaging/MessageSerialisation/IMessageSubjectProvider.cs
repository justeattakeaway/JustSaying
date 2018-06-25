using System;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSubjectProvider
    {
        string GetSubjectForType(Type messageType);
    }
}
