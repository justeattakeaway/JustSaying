using System.Collections.Generic;
using System.Linq;
using SimplesNotificationStack.Messaging.Messages;

namespace SimplesNotificationStack.Messaging.MessageSerialisation
{
    public static class SerialisationMap
    {
        private static readonly List<IMessageSerialiser<Message>> Map = new List<IMessageSerialiser<Message>>();

        internal static void Register(IMessageSerialiser<Message> serialisation)
        {
            Map.Add(serialisation);
        }

        public static bool IsRegistered { get { return Map.Count > 0; } }

        public static IMessageSerialiser<Message> GetSerialiser(string objectType)
        {
            return Map.FirstOrDefault(x => x.Key == objectType);
        }
    }
}
