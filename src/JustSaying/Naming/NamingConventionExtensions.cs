namespace JustSaying.Naming
{
    public static class NamingConventionExtensions
    {
        /// <summary>
        /// Applies an <see cref="ITopicNamingConvention"/> to a type <typeparamref name="T"/> and returns the result, with an optional override.
        /// </summary>
        /// <param name="namingConvention">An <see cref="ITopicNamingConvention"/> to apply to the <typeparamref name="T"/>.</param>
        /// <param name="overrideTopicName">An override that will be returned instead of the naming convention
        /// if the override is not null or whitespace.</param>
        /// <typeparam name="T">A type from which a topic name will be determined using the supplied <see cref="ITopicNamingConvention"/>.</typeparam>
        /// <returns>A string that is either the override if supplied, or the <see cref="ITopicNamingConvention"/> applied to the <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see langword="null"/>.
        /// </exception>
        public static string Apply<T>(this ITopicNamingConvention namingConvention, string overrideTopicName)
        {
            if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

            return string.IsNullOrWhiteSpace(overrideTopicName)
                ? namingConvention.TopicName<T>()
                : overrideTopicName;
        }

        /// <summary>
        /// Applies an <see cref="IQueueNamingConvention"/> to a type <typeparamref name="T"/> and returns the result, with an optional override.
        /// </summary>
        /// <param name="namingConvention">An <see cref="IQueueNamingConvention"/> to apply to the <typeparamref name="T"/>.</param>
        /// <param name="overrideQueueName">An override that will be returned instead of the naming convention
        /// if the override is not null or whitespace.</param>
        /// <typeparam name="T">A type from which a queue name will be determined using the supplied <see cref="IQueueNamingConvention"/>.</typeparam>
        /// <returns>A string that is either the override if supplied, or the <see cref="IQueueNamingConvention"/> applied to the <typeparamref name="T"/>.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="namingConvention"/> is <see langword="null"/>.
        /// </exception>
        public static string Apply<T>(this IQueueNamingConvention namingConvention, string overrideQueueName)
        {
            if (namingConvention == null) throw new ArgumentNullException(nameof(namingConvention));

            return string.IsNullOrWhiteSpace(overrideQueueName)
                ? namingConvention.QueueName<T>()
                : overrideQueueName;
        }
    }
}
