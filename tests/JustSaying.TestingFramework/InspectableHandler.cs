using System;
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
            ShouldSucceed = true;
        }

        public Action<T> OnHandle { get; set; }
        public IList<T> ReceivedMessages { get; }

        public bool ShouldSucceed { get; set; }

        public virtual Task<bool> Handle(T message)
        {
            ReceivedMessages.Add(message);

            OnHandle?.Invoke(message);

            return Task.FromResult(ShouldSucceed);
        }
    }
}
