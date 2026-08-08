namespace JustSaying.Fluent;

/// <summary>
/// The destination of a queue registration: which SQS queue it targets, and — when JustSaying owns
/// the queue — how to create it. A queue is owned (created on startup) when it is named by
/// convention or explicitly; a queue addressed by URL or ARN already exists and is never created,
/// so it offers no infrastructure configuration. This class cannot be inherited.
/// </summary>
public sealed class Queue
{
    private Queue()
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
    /// <returns>The <see cref="Queue"/> destination.</returns>
    public static Queue ByConvention() => new();

    /// <summary>
    /// Targets a queue named by the queue naming convention applied to the message type, configuring
    /// how it is created.
    /// </summary>
    /// <param name="configure">A delegate to configure the queue's infrastructure.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static Queue ByConvention(Action<QueueInfrastructure> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new QueueInfrastructure();
        configure(infrastructure);

        return new Queue { Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a queue with the specified name. The queue is created on startup if it does not exist.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    public static Queue Named(string name)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(name));

        return new Queue { Name = name };
    }

    /// <summary>
    /// Targets a queue with the specified name, configuring how it is created.
    /// </summary>
    /// <param name="name">The name of the queue.</param>
    /// <param name="configure">A delegate to configure the queue's infrastructure.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="configure"/> is <see langword="null"/>.</exception>
    public static Queue Named(string name, Action<QueueInfrastructure> configure)
    {
        if (string.IsNullOrEmpty(name)) throw new ArgumentException("Parameter cannot be null or empty.", nameof(name));
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var infrastructure = new QueueInfrastructure();
        configure(infrastructure);

        return new Queue { Name = name, Infrastructure = infrastructure };
    }

    /// <summary>
    /// Targets a pre-existing queue by its URL. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (for example <c>eu-west-1</c>); when omitted, the region is inferred from the URL.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    public static Queue FromUri(Uri queueUrl, string regionName = null) => new() { Address = QueueAddress.FromUri(queueUrl, regionName) };

    /// <summary>
    /// Targets a pre-existing queue by its URL. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueUrl">The queue URL.</param>
    /// <param name="regionName">Optional region name (for example <c>eu-west-1</c>); when omitted, the region is inferred from the URL.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    public static Queue FromUrl(string queueUrl, string regionName = null) => new() { Address = QueueAddress.FromUrl(queueUrl, regionName) };

    /// <summary>
    /// Targets a pre-existing queue by its ARN. The queue is never created by JustSaying, so no
    /// infrastructure configuration is available.
    /// </summary>
    /// <param name="queueArn">The queue ARN.</param>
    /// <returns>The <see cref="Queue"/> destination.</returns>
    public static Queue FromArn(string queueArn) => new() { Address = QueueAddress.FromArn(queueArn) };

    internal static Queue FromAddress(QueueAddress address) => new() { Address = address };
}
