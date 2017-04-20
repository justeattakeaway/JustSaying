using JustSaying.Models;

namespace JustSaying.TestingFramework
{
    public class OrderAccepted : Message
    { }

    public class OrderRejected : Message
    { }

    public class GenericMessage : Message
    {
        public string Content { get; set; }
    }

    public class AnotherGenericMessage : Message
    {

        public AnotherGenericMessage() 
        {
        }

        public string Content { get; set; }
    }

    public class DelayedMessage : Message
    {
        public DelayedMessage(int delaySeconds)
        {
            DelaySeconds = delaySeconds;
        }
    }

    public class MessageWithEnum : Message
    {
        public MessageWithEnum(Values enumVal)
        {
            EnumVal = enumVal;
        }

        public Values EnumVal { get; private set; }
    }

    public enum Values { One, Two };

    public class DownStreamMessage : Message
    {
        public DownStreamMessage(Message message) : base(message)
        {
        }
    }
}