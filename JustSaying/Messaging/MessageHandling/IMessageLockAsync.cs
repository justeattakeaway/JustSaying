using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace JustSaying.Messaging.MessageHandling
{
    public interface IMessageLockAsync
    {
        Task<MessageLockResponse> TryAquireLockPermanentlyAsync(string key);
        Task<MessageLockResponse> TryAquireLockAsync(string key, TimeSpan howLong);
        Task ReleaseLockAsync(string key);
    }
}
