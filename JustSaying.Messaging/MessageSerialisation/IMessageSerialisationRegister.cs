using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationRegister
    {
        TypeSerialiser GeTypeSerialiser(string objectType);
        TypeSerialiser GeTypeSerialiser(Type objectType);
        void AddSerialiser<T>(IMessageSerialiser serialiser) where T : Message;
    }
}