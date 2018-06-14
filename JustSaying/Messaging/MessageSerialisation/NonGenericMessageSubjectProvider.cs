using System;

namespace JustSaying.Messaging.MessageSerialisation
{
    /// <summary>
    /// This implementation is not suitable for generic types,
    /// but replicates the behaviour of the system before IMessageSubjectProvider was introduced
    /// </summary>
    public class NonGenericMessageSubjectProvider : IMessageSubjectProvider
    {
        public string GetTypeForSubject(Type messageType) => messageType.Name;
    }
}
