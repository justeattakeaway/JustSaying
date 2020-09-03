using System.Collections.Generic;
using System.Threading.Tasks;
using JustSaying.Messaging.MessageHandling;

namespace JustSaying.TestingFramework
{


    public class InspectableHandler<T> : IHandlerAsync<T>
    {
        public InspectableHandler()
        {
            ReceivedMessages = new List<T>();
        }

        public IList<T> ReceivedMessages { get; }

        public virtual Task<bool> Handle(T message)
        {
            ReceivedMessages.Add(message);
            return Task.FromResult(true);
        }
    }
}
