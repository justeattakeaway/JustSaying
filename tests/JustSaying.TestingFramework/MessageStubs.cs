using System;

namespace JustSaying.TestingFramework
{
    public abstract class Message
    {
        protected Message()
        {
            Id = Guid.NewGuid();
        }

        public Guid Id { get; set; }

        public string UniqueKey() => Id.ToString();
    }

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
        public string RaisingComponent { get; set; }
    }

    public class AnotherSimpleMessage : Message
    {
        public string Content { get; set; }
    }

    public class MessageWithEnum : Message
    {
        public MessageWithEnum()
        {
        }

        public Value EnumVal { get; set; }
    }

    public enum Value
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
