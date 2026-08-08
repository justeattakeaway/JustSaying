namespace JustSaying.Fluent;

/// <summary>
/// The destination of a topic publication: which SNS topic it targets, and — when JustSaying owns
/// the topic — how to create it. A topic is owned (created on startup) when it is named by
/// convention or explicitly; a topic addressed by ARN already exists and is never created, so it
/// offers no infrastructure configuration. This class cannot be inherited.
/// </summary>
public sealed class Topic
{
    private Topic()
    { }

    /// <summary>
    /// Gets the explicit topic name, or <see langword="null"/> when named by convention or addressed.
    /// </summary>
    internal string Name { get; private set; }

    /// <summary>
    /// Gets the address of a pre-existing topic, or <see langword="null"/> when JustSaying owns it.
    /// </summary>
    internal TopicAddress Address { get; private set; }

    /// <summary>
    /// Gets the configuration for creating the topic, when JustSaying owns it.
    /// </summary>
    internal TopicInfrastructure Infrastructure { get; private set; }

    internal bool IsAddress => Address is not null;

    /// <summary>
    /// Targets a topic named by the topic naming convention applied to the message type. The topic is
    /// created on startup if it does not exist.
    /// </summary>
    /// <returns>The <see cref="Topic"/> destination.</returns>
    public static Topic ByConvention() => new();

    /// <summary>
    /// Targets a topic named by the topic naming convention applied to the message type, configuring
    /// how it is created.
    /// </summary>
    /// <param name="configure">A delegate to configure the topic's infrastructure.</param>
    /// <returns>The <see cref="Topic"/> destination.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static Topic ByConvention(Action<TopicInfrastructure> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new TopicInfrastructure();
        configure(infrastructure);

        return new Topic { Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a topic with the specified name. The topic is created on startup if it does not exist.
    /// </summary>
    /// <param name="name">The name of the topic.</param>
    /// <returns>The <see cref="Topic"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static Topic Named(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(name));

        return new Topic { Name = name };
    }

    /// <summary>
    /// Targets a topic with the specified name, configuring how it is created.
    /// </summary>
    /// <param name="name">The name of the topic.</param>
    /// <param name="configure">A delegate to configure the topic's infrastructure.</param>
    /// <returns>The <see cref="Topic"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static Topic Named(string name, Action<TopicInfrastructure> configure)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(name));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new TopicInfrastructure();
        configure(infrastructure);

        return new Topic { Name = name, Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a pre-existing topic by its ARN. The topic is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="topicArn">The SNS topic ARN.</param>
    /// <returns>The <see cref="Topic"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="topicArn"/> is not a valid SNS topic ARN.</exception>
    public static Topic FromArn(string topicArn) => new() { Address = TopicAddress.FromArn(topicArn) };
}
