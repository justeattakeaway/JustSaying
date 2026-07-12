using SQSMessage = Amazon.SQS.Model.Message;

namespace JustSaying.Messaging.MessageHandling;

/// <summary>
/// Creates the <see cref="MessageContext"/> for the message currently being processed, allowing a
/// serializer whose wire format carries envelope metadata (such as a CloudEvents envelope) to
/// substitute a derived context that exposes that metadata to handlers via
/// <see cref="IMessageContextReader"/>.
/// </summary>
/// <param name="message">The <see cref="Amazon.SQS.Model.Message"/> currently being processed.</param>
/// <param name="queueUri">The URI of the SQS queue the message is from.</param>
/// <param name="messageAttributes">The <see cref="MessageAttributes"/> from the message.</param>
/// <returns>The <see cref="MessageContext"/> (or derived type) for the current message.</returns>
public delegate MessageContext MessageContextFactory(SQSMessage message, Uri queueUri, MessageAttributes messageAttributes);
