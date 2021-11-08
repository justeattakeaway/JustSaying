namespace JustSaying.Messaging.MessageHandling;

public interface IMessageLockAsync
{
    Task<MessageLockResponse> TryAcquireLockPermanentlyAsync(string key);
    Task<MessageLockResponse> TryAcquireLockAsync(string key, TimeSpan howLong);
    Task ReleaseLockAsync(string key);
}