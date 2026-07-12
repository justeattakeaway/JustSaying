using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging.Middleware;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic publication. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicPublicationBuilder<T> : IPublicationBuilder<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TopicPublicationBuilder{T}"/> class.
    /// </summary>
    internal TopicPublicationBuilder()
    { }

    /// <summary>
    /// Gets or sets a delegate to a method to use to configure SNS writes.
    /// </summary>
    private Action<SnsWriteConfiguration> ConfigureWrites { get; set; }

    /// <summary>
    /// Gets the tags to add to the topic.
    /// </summary>
    private Dictionary<string, string> Tags { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the topic name.
    /// </summary>
    private string TopicName { get; set; } = string.Empty;

    private Action<PublishMiddlewareBuilder> MiddlewareConfiguration { get; set; }

    /// <summary>
    /// An optional custom serializer built from the bus's serialization factory, used instead of the
    /// factory's per-type default. Internal extensibility seam for serializer packages (such as
    /// JustSaying.CloudEvents, which exposes it via <c>WithCloudEvent&lt;T&gt;</c>).
    /// </summary>
    internal Func<JustSaying.Messaging.MessageSerialization.IMessageBodySerializationFactory, JustSaying.Messaging.MessageSerialization.IMessageBodySerializer<T>> SerializerOverride { get; set; }

    /// <summary>
    /// An optional resolver for the topic name, applied when no explicit name is set — instead of the
    /// naming convention keyed on <typeparamref name="T"/>. Internal extensibility seam used by wrapper
    /// publications (such as CloudEvents envelopes) so the topic is named after the payload type rather
    /// than the wrapper type.
    /// </summary>
    internal Func<JustSaying.Naming.ITopicNamingConvention, string> TopicNameResolver { get; set; }

    /// <summary>
    /// Function that will produce a topic name dynamically from a message at publish time.
    /// If the topic doesn't exist, it will be created at that point.
    /// </summary>
    public Func<T, string> TopicNameCustomizer { get; set; }

    /// <summary>
    /// Configures the SNS write configuration.
    /// </summary>
    /// <param name="configure">A delegate to a method to use to configure SNS writes.</param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public TopicPublicationBuilder<T> WithWriteConfiguration(
        Action<SnsWriteConfigurationBuilder> configure)
    {
        if (configure == null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        var builder = new SnsWriteConfigurationBuilder();

        configure(builder);

        ConfigureWrites = builder.Configure;
        return this;
    }

    /// <summary>
    /// Configures the SNS write configuration.
    /// </summary>
    /// <param name="configure">A delegate to a method to use to configure SNS writes.</param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="configure"/> is <see langword="null"/>.
    /// </exception>
    public TopicPublicationBuilder<T> WithWriteConfiguration(Action<SnsWriteConfiguration> configure)
    {
        ConfigureWrites = configure ?? throw new ArgumentNullException(nameof(configure));
        return this;
    }

    /// <summary>
    /// Creates a tag with no value that will be assigned to the SNS topic.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public TopicPublicationBuilder<T> WithTag(string key) => WithTag(key, null);

    /// <summary>
    /// Creates a tag with a value that will be assigned to the SNS topic.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The value associated with this tag.</param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    /// <remarks>Tag keys are case-sensitive. A new tag with a key identical to that of an existing one will overwrite it.</remarks>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is <see langword="null"/> or whitespace.
    /// </exception>
    public TopicPublicationBuilder<T> WithTag(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("A topic tag key cannot be null or only whitespace", nameof(key));
        }

        Tags.Add(key, value ?? string.Empty);

        return this;
    }

    /// <summary>
    /// Configures the name of the topic.
    /// </summary>
    /// <param name="name">The name of the topic to publish to.</param>
    /// <returns>
    /// The current <see cref="TopicSubscriptionBuilder{T}"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="name"/> is <see langword="null"/>.
    /// </exception>
    public TopicPublicationBuilder<T> WithTopicName(string name)
    {
        TopicName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <summary>
    /// Configures the name of the topic by calling this func at publish time to determine the name of the topic.
    /// If the topic does not exist, it will be created on first publish.
    /// </summary>
    /// <param name="topicNameCustomizer">Function that will be called at publish time to determine the name of the target topic for this <see cref="T"/>.
    /// <para>
    /// For example: <c>WithTopicName(msg => $"{msg.Tenant}-mymessage")</c> with <c>msg.Tenant</c> of <c>["uk", "au"]</c> would
    /// create topics <c>"uk-mymessage"</c> and <c>"au-mymessage"</c> when a message is published with those tenants.
    /// </para>
    /// </param>
    /// <returns>
    /// The current <see cref="TopicPublicationBuilder{T}"/>.
    /// </returns>
    public TopicPublicationBuilder<T> WithTopicName(Func<T, string> topicNameCustomizer)
    {
        TopicNameCustomizer = topicNameCustomizer;
        return this;
    }

    /// <summary>
    /// Configures the publish middleware pipeline for this publication.
    /// </summary>
    /// <param name="middlewareConfiguration">A delegate to configure the publish middleware pipeline.</param>
    /// <returns>The current <see cref="TopicPublicationBuilder{T}"/>.</returns>
    public TopicPublicationBuilder<T> WithMiddlewareConfiguration(
        Action<PublishMiddlewareBuilder> middlewareConfiguration)
    {
        MiddlewareConfiguration = middlewareConfiguration;
        return this;
    }

    /// <inheritdoc />
    void IPublicationBuilder<T>.Configure(
        JustSayingBus bus,
        IAwsClientFactoryProxy proxy,
        ILoggerFactory loggerFactory,
        IServiceResolver serviceResolver)
    {
        var logger = loggerFactory.CreateLogger<TopicPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
            typeof(T));

        var region = bus.Config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(bus.Config.Region)} property.");

        var writeConfiguration = new SnsWriteConfiguration();
        ConfigureWrites?.Invoke(writeConfiguration);
        writeConfiguration.CompressionOptions ??= bus.Config.DefaultCompressionOptions;
        CompressionEncodingValidator.ValidateEncoding(bus.CompressionRegistry, writeConfiguration.CompressionOptions);

        var client = proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region));

        StaticPublicationConfiguration BuildConfiguration(string topicName)
            => StaticPublicationConfiguration.Build<T>(topicName,
                Tags,
                writeConfiguration,
                client,
                loggerFactory,
                bus,
                SerializerOverride);

        var topicName = TopicName;
        if (string.IsNullOrEmpty(topicName) && TopicNameResolver is not null)
        {
            topicName = TopicNameResolver(bus.Config.TopicNamingConvention);
        }

        ITopicPublisher config = TopicNameCustomizer != null
            ? DynamicPublicationConfiguration.Build<T>(message => TopicNameCustomizer((T)message), BuildConfiguration, loggerFactory)
            : BuildConfiguration(topicName);

        bus.AddStartupTask(config.StartupTask);
        bus.AddMessagePublisher<T>(config.Publisher);
        bus.AddMessageBatchPublisher<T>(config.BatchPublisher);

        if (MiddlewareConfiguration != null)
        {
            var middlewareBuilder = new PublishMiddlewareBuilder(serviceResolver);
            middlewareBuilder.Configure(MiddlewareConfiguration);
            bus.AddPublishMiddleware<T>(middlewareBuilder.Build());
        }
    }
}
