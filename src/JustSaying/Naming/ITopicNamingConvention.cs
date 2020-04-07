using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Naming
{
    /// <summary>
    /// Defines a method for creating a topic name.
    /// </summary>
    public interface ITopicNamingConvention
    {
        /// <summary>
        /// Returns the topic name to use.
        /// </summary>
        /// <typeparam name="T">
        /// The message type
        /// </typeparam>
        /// <returns>The topic name that will be used for the message type.</returns>
        string TopicName<T>(string topicNameOverride);
    }
}
