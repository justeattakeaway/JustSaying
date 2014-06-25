using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationRegister
    {
        IMessageSerialiser<Message> GetSerialiser(string objectType);
        IMessageSerialiser<Message> GetSerialiser(Type objectType);
        void AddSerialiser<T>(IMessageSerialiser<Message> serialiser) where T : Message;
    }
}