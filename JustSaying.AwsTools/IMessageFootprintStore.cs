using System;

namespace JustSaying.AwsTools
{
    public interface IMessageFootprintStore
    {
        bool IsMessageReceieved(Guid messageId);
        void MarkMessageAsRecieved(Guid messageId);
    }

    public class NullMessageFootprintStore : IMessageFootprintStore
    {
        public bool IsMessageReceieved(Guid messageId)
        {
            return false;
        }

        public void MarkMessageAsRecieved(Guid messageId)
        {
        }
    }
}
