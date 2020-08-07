using System;

namespace JustSaying.Naming
{
    public static class NamingConventionExtensions
    {
        public static string Apply<T>(this ITopicNamingConvention namingConvention, string overrideTopicName)
        {
            if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

            return string.IsNullOrWhiteSpace(overrideTopicName)
                ? namingConvention.TopicName<T>()
                : overrideTopicName;
        }

        public static string Apply<T>(this IQueueNamingConvention namingConvention, string overrideQueueName)
        {
            if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

            return string.IsNullOrWhiteSpace(overrideQueueName)
                ? namingConvention.QueueName<T>()
                : overrideQueueName;
        }
    }
}
