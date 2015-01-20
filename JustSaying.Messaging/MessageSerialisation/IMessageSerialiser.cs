using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser
    {
        Message Deserialise(string message, Type type);
        string Serialise(Message message);
    }
}