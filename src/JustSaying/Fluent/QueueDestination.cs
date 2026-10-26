using JustSaying.AwsTools.QueueCreation;

namespace JustSaying.Fluent;

/// <summary>
/// The destination of a queue registration: which SQS queue it targets, and — when JustSaying owns
/// the queue — how to create it. A queue is owned (created on startup) when it is named by
/// convention or explicitly; a queue addressed by URL or ARN already exists and is never created,
/// so it offers no infrastructure configuration. This class cannot be inherited.
/// </summary>
public sealed class QueueDestination
{
    private QueueDestination()
    { }

    /// <summary>
    /// Gets the explicit queue name, or <see langword="null"/> when named by convention or addressed.
    /// </summary>
    internal string Name { get; private set; }

    /// <summary>
    /// Gets the address of a pre-existing queue, or <see langword="null"/> when JustSaying owns it.
    /// </summary>
    internal QueueAddress Address { get; private set; }

    /// <summary>
    /// Gets the configuration for creating the queue, when JustSaying owns it.
    /// </summary>
    internal QueueInfrastructure Infrastructure { get; private set; }

    internal bool IsAddress => Address is not null;

    /// <summary>
    /// Targets a queue named by the queue naming convention applied to the message type. The queue is
    /// created on startup if it does not exist.
    /// </summary>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    public static QueueDestination ByConvention() => new();

    /// <summary>
    /// Targets a queue named by the queue naming convention applied to the message type, configuring
    /// how it is created.
    /// </summary>
    /// <param name="configure">A delegate to configure the queue's infrastructure.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="configure"/> configures an error queue it also opts out of.</exception>
    public static QueueDestination ByConvention(Action<QueueInfrastructure> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new QueueInfrastructure();
        configure(infrastructure);
        infrastructure.Validate();

        return new QueueDestination { Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a queue with the specified name. The queue is created on startup if it does not exist.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid queue name.</exception>
    public static QueueDestination Named(string name)
    {
        ValidateName(name, hasErrorQueue: true);

        return new QueueDestination { Name = name };
    }

    /// <summary>
    /// Targets a queue with the specified name, configuring how it is created.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configure">A delegate to configure the queue's infrastructure.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is not a valid queue name.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException"><paramref name="configure"/> configures an error queue it also opts out of.</exception>
    public static QueueDestination Named(string name, Action<QueueInfrastructure> configure)
    {
        ValidateName(name, hasErrorQueue: false);
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new QueueInfrastructure();
        configure(infrastructure);
        infrastructure.Validate();

        // The error queue's name only counts towards the length limit once we know there is one.
        ValidateName(name, hasErrorQueue: !infrastructure.ErrorQueueOptOut);

        return new QueueDestination { Name = name, Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a pre-existing queue by its URL. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (for example <c>eu-west-1</c>); when omitted, the region is inferred from the URL.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="queueUrl"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="queueUrl"/> is not an SQS queue URL, or <paramref name="regionName"/> contradicts the region in it.</exception>
    public static QueueDestination FromUri(Uri queueUrl, string regionName = null) => new() { Address = QueueAddress.FromUri(queueUrl, regionName) };

    /// <summary>
    /// Targets a pre-existing queue by its URL. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (for example <c>eu-west-1</c>); when omitted, the region is inferred from the URL.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="queueUrl"/> is not an SQS queue URL, or <paramref name="regionName"/> contradicts the region in it.</exception>
    public static QueueDestination FromUrl(string queueUrl, string regionName = null) => new() { Address = QueueAddress.FromUrl(queueUrl, regionName) };

    /// <summary>
    /// Targets a pre-existing queue by its ARN. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueArn">The queue ARN.</param>
    /// <returns>The <see cref="QueueDestination"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="queueArn"/> is not a complete SQS queue ARN.</exception>
    public static QueueDestination FromArn(string queueArn) => new() { Address = QueueAddress.FromArn(queueArn) };

    private static void ValidateName(string name, bool hasErrorQueue)
    {
        if (ResourceNameValidator.GetQueueNameError(name, hasErrorQueue) is { } error)
        {
            throw new ArgumentException(error, nameof(name));
        }
    }
}
