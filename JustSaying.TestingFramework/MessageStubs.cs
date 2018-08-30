using JustSaying.Models;

namespace JustSaying.TestingFramework
{
    public class Order : Message
    {
    }

    public class OrderAccepted : Message
    {
    }

    public class OrderRejected : Message
    {
    }

    public class SimpleMessage : Message
    {
        public string Content { get; set; }
    }

    public class AnotherSimpleMessage : Message
    {
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

    public enum Values
    {
        One,
        Two
    }

    public class GenericMessage<T> : Message
    {
        public T Contents { get; set; }
    }

    public class MyMessage
    {
    }
}
