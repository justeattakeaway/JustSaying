using JustSaying.Messaging.Messages;

namespace JustSaying.Tests.MessageStubs
{
    public class OrderAccepted : Message
    { }

    public class OrderRejected : Message
    { }

    public class GenericMessage : Message
    { }

    public class MessageWithEnum : Message
    {
        public MessageWithEnum(Values enumVal)
        {
            EnumVal = enumVal;
        }

        public Values EnumVal { get; private set; }
    }

    public enum Values { One, Two };
}