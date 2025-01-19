using JustSaying.Messaging;

namespace JustSaying.Fluent;

internal interface ITopicAddressPublisher
{
    IMessagePublisher Publisher { get; }
    IMessageBatchPublisher BatchPublisher { get; }
}
