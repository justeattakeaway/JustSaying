using System;
using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialiser
    {
        string GetMessageSubject(string sqsMessge);
        Message Deserialise(string message, Type type);

        /// <summary>
        /// Serialises a message for publishing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="serializeForSnsPublishing">If set to false, then message will be wrapped in extra object with Subject and Message fields, e.g.:
        /// new { Subject = subject, Message = serializedMessage };
        /// 
        /// AWS SNS service adds these automatically, so for publishing to topics don't add these properties
        /// </param>
        /// <returns></returns>
        string Serialise(Message message, bool serializeForSnsPublishing, string subject);
    }
}
