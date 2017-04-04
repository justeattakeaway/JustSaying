using JustSaying.Models;

namespace JustSaying.Messaging.MessageSerialisation
{
    public interface IMessageSerialisationRegister
    {
        /// <summary>
        /// Deserializes a message.
        /// </summary>
        /// <param name="body">Message must always have Subject and Message properties</param>
        /// <returns></returns>
        Message DeserializeMessage(string body);

        /// <summary>
        /// Serializes a message for publishing
        /// </summary>
        /// <param name="message"></param>
        /// <param name="serializeForSnsPublishing">If set to false, then message will be wrapped in extra object with Subject and Message fields, e.g.:
        /// new { Subject = message.GetType().Name, Message = serializedMessage };
        /// 
        /// AWS SNS service adds these automatically, so for publishing to topics don't add these properties
        /// </param>
        /// <returns></returns>
        string Serialise(Message message, bool serializeForSnsPublishing);

        void AddSerialiser<T>(IMessageSerialiser serialiser) where T : Message;
    }
}