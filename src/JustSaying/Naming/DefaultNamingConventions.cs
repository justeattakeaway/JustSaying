using JustSaying.Extensions;

namespace JustSaying.Naming
{
    public class DefaultNamingConventions : IDefaultTopicNamingConvention, IDefaultQueueNamingConvention
    {
        public string TopicName<T>() => typeof(T).ToDefaultTopicName();

        public string QueueName<T>() => typeof(T).ToDefaultQueueName();
    }
}
