namespace JustSaying.Messaging.MessageSerialization
{
    public interface IMessageSerializationRegister
    {
        /// <summary>
        /// Deserializes a message.
        /// </summary>
        /// <param name="body">Message must always have Subject and Message properties</param>
        /// <returns></returns>
        object DeserializeMessage(string body);

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
        string Serialize<T>(T message, bool serializeForSnsPublishing) where T : class;

        void AddSerializer<T>(IMessageSerializer serializer) where T : class;
    }
}
