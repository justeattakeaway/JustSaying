using System.Threading;

namespace JustSaying.Messaging.MessageHandling
{
    public class MessageContextReader: IMessageContextReader
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
