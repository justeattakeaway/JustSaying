using JustSaying.Messaging.Interrogation;
using Microsoft.Extensions.Logging;

namespace JustSaying.Messaging.Channels.SubscriptionGroups;

/// <inheritdoc />
/// <summary>
/// Runs multiple instances of <see cref="SubscriptionGroup"/>.
/// </summary>
/// <param name="subscriptionGroups">The collection of <see cref="SubscriptionGroup"/> instances to run.</param>
/// <param name="logger">The <see cref="ILogger"/> to use.</param>
public class SubscriptionGroupCollection(
    IList<ISubscriptionGroup> subscriptionGroups,
    ILogger<SubscriptionGroupCollection> logger) : ISubscriptionGroup
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IList<ISubscriptionGroup> _subscriptionGroups = subscriptionGroups ?? throw new ArgumentNullException(nameof(subscriptionGroups));
    private Task _completion;
    private bool _started;
    private readonly object _startLock = new();

    /// <inheritdoc />
    public Task RunAsync(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
        {
            return Task.CompletedTask;
        }

        if (!_started)
        {
            lock (_startLock)
            {
                if (!_started)
                {
                    _completion = RunImplAsync(stoppingToken);
                    _started = true;
                }
            }
        }

        return _completion;
    }

    /// <inheritdoc />
    public InterrogationResult Interrogate()
    {
        return new InterrogationResult(new
        {
            Groups = _subscriptionGroups.Select(group => group.Interrogate()).ToArray(),
        });
    }

    private Task RunImplAsync(CancellationToken stoppingToken)
    {
        IEnumerable<Task> completionTasks = _subscriptionGroups.Select(group => group.RunAsync(stoppingToken)).ToList();

        _logger.LogInformation("Subscription group collection successfully started");

        return Task.WhenAll(completionTasks);
    }
}
