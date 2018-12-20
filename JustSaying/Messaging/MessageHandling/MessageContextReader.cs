using System.Threading;

namespace JustSaying.Messaging.MessageHandling
{
#pragma warning disable CA1822 // Member Read does not access instance data and can be marked as static

    public class MessageContextReader
    {
        private static readonly AsyncLocal<MessageContext> Context = new AsyncLocal<MessageContext>();

        public MessageContext Read()
        {
            return Context.Value;
        }

        internal static void Write(MessageContext value)
        {
            Context.Value = value;
        }
    }
}
