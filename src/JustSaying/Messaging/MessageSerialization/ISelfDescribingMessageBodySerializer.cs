namespace JustSaying.Messaging.MessageSerialization;

/// <summary>
/// Marks an <see cref="IMessageBodySerializer{TMessage}"/> whose serialized output is self-describing:
/// it already carries the metadata (such as the message type) that JustSaying would otherwise add by
/// wrapping a point-to-point queue message in its <c>{ "Message", "Subject" }</c> envelope.
/// </summary>
/// <remarks>
/// When a queue publication's serializer implements this interface, JustSaying writes the serialized
/// body to the queue verbatim — as though <c>WithRawMessages()</c> had been configured — so the body
/// is not double-wrapped. This is used, for example, by the CloudEvents serializer, whose structured
/// envelope already conveys the <c>type</c>, <c>source</c> and <c>id</c> of each message.
/// </remarks>
public interface ISelfDescribingMessageBodySerializer
{
}
