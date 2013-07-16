using System;
using JustEat.Simples.NotificationStack.Messaging.Messages;

namespace JustEat.Simples.NotificationStack.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationRegister
    {
        IMessageSerialiser<Message> GetSerialiser(string objectType);
        IMessageSerialiser<Message> GetSerialiser(Type objectType);
    }
}