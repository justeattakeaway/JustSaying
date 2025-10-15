using System.Collections.Generic;
using JustSaying.Messaging.Compression;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public class NewtonsoftSerialisationFactory : IMessageSerialisationFactory
    {
        public IMessageSerialiser GetSerialiser<T>() where T : Message
        {
            return new NewtonsoftSerialiser()
                .AddCompression(CompressedHeaders.GzipBase64Header, new GzipMessageBodyCompression());
        }

        public IMessageSerialiser GetSerialiser<T>(Dictionary<string, IMessageBodyCompression> compressions) where T : Message
        {
            return new NewtonsoftSerialiser().AddCompressions(compressions);
        }
    }
}
