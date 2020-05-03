using System;

namespace JustSaying.Messaging.MessageSerialization
{
    public interface IMessageSerializer
    {
        string GetMessageSubject(string sqsMessage);

        object Deserialize(string message, Type type);

        /// <summary>
        /// Serializes a message for publishing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="serializeForSnsPublishing">If set to false, then message will be wrapped in extra object with Subject and Message fields, e.g.:
        /// new { Subject = subject, Message = serializedMessage };
        ///
        /// AWS SNS service adds these automatically, so for publishing to topics don't add these properties
        /// </param>
        /// <returns></returns>
        string Serialize(object message, bool serializeForSnsPublishing, string subject);
    }
}
