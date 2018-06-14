using System;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSubjectProvider
    {
        string GetTypeForSubject(Type messageType);
    }
}
