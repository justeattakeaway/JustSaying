using Amazon;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Models;
using Microsoft.Extensions.Logging;

namespace JustSaying.Fluent;

/// <summary>
/// A class representing a builder for a topic publication. This class cannot be inherited.
/// </summary>
/// <typeparam name="T">
/// The type of the message.
/// </typeparam>
public sealed class TopicPublicationBuilder<T> : IPublicationBuilder<T>
    where T : Message
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
    public TopicPublicationBuilder<T> WithName(string name)
    {
        TopicName = name ?? throw new ArgumentNullException(nameof(name));
        return this;
    }

    /// <inheritdoc />
    void IPublicationBuilder<T>.Configure(
        JustSayingBus bus,
        IAwsClientFactoryProxy proxy,
        ILoggerFactory loggerFactory)
    {
        var logger = loggerFactory.CreateLogger<TopicPublicationBuilder<T>>();

        logger.LogInformation("Adding SNS publisher for message type '{MessageType}'.",
            typeof(T));

        var config = bus.Config;
        var region = config.Region ?? throw new InvalidOperationException($"Config cannot have a blank entry for the {nameof(config.Region)} property.");

        var readConfiguration = new SqsReadConfiguration(SubscriptionType.ToTopic)
        {
            TopicName = TopicName
        };
        var writeConfiguration = new SnsWriteConfiguration();
        ConfigureWrites?.Invoke(writeConfiguration);
        readConfiguration.ApplyTopicNamingConvention<T>(config.TopicNamingConvention);

        bus.SerializationRegister.AddSerializer<T>();

        var eventPublisher = new SnsMessagePublisher(
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
            bus.SerializationRegister,
            loggerFactory,
            config.MessageSubjectProvider)
        {
            MessageResponseLogger = config.MessageResponseLogger,
        };

#pragma warning disable 618
        var snsTopic = new SnsTopicByName(
            readConfiguration.TopicName,
            proxy.GetAwsClientFactory().GetSnsClient(RegionEndpoint.GetBySystemName(region)),
            loggerFactory)
        {
            Tags = Tags
        };
#pragma warning restore 618

        async Task StartupTask(CancellationToken cancellationToken)
        {
            if (writeConfiguration.Encryption != null)
            {
                await snsTopic.CreateWithEncryptionAsync(writeConfiguration.Encryption, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                await snsTopic.CreateAsync(cancellationToken).ConfigureAwait(false);
            }

            await snsTopic.EnsurePolicyIsUpdatedAsync(config.AdditionalSubscriberAccounts)
                .ConfigureAwait(false);

            await snsTopic.ApplyTagsAsync(cancellationToken).ConfigureAwait(false);

            eventPublisher.Arn = snsTopic.Arn;
        }

        bus.AddStartupTask(StartupTask);

        bus.AddMessagePublisher<T>(eventPublisher);

        logger.LogInformation(
            "Created SNS topic publisher on topic '{TopicName}' for message type '{MessageType}'.",
            readConfiguration.TopicName,
            typeof(T));
    }
}
