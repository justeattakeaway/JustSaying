using System.Collections.Generic;
using JustSaying.Messaging.Compression;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationFactory
    {
        IMessageSerialiser GetSerialiser<T>() where T : Message;
        IMessageSerialiser GetSerialiser<T>(Dictionary<string, IMessageBodyCompression> compressions) where T : Message;
    }
}
