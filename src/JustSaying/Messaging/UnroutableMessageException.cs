namespace JustSaying.Messaging;

/// <summary>
/// Thrown when an inbound message can't be matched to a registration that can read it, for example a
/// type a multi-type queue doesn't handle. Unlike <see cref="MessageSerialization.MessageFormatNotSupportedException"/>
/// the message is not deleted: it is left on the queue so the redrive policy moves it to the error
/// queue, where it can be inspected and redriven once a consumer can handle it.
/// </summary>
internal sealed class UnroutableMessageException(string message) : Exception(message);
