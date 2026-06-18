using System.Diagnostics;
using Amazon;
using Amazon.Runtime;
using Amazon.SimpleNotificationService.Model;
using JustSaying.AwsTools;
using JustSaying.AwsTools.MessageHandling;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.IntegrationTests.Aspire;
using JustSaying.Models;
using JustSaying.TestingFramework;
using LocalSqsSnsMessaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.IntegrationTests.Fluent;

public abstract class IntegrationTestBase
{
    /// <summary>
    /// The Aspire fixture that hosts the floci container when floci mode is enabled.
    /// Shared across the whole test session, and a no-op in the default in-memory mode.
    /// </summary>
    [ClassDataSource<AspireFixture>(Shared = SharedType.PerTestSession)]
    public required AspireFixture AspireFixture { get; init; }

    protected virtual string AccessKeyId { get; } = "accessKeyId";

    protected virtual string SecretAccessKey { get; } = "secretAccessKey";

    protected virtual string SessionToken { get; } = "token";

    protected TextWriter OutputHelper => TestContext.Current!.OutputWriter;

    protected ILoggerFactory LoggerFactory => _loggerFactory ??= Microsoft.Extensions.Logging.LoggerFactory.Create(lf => lf.AddTextWriter(OutputHelper));
    private ILoggerFactory _loggerFactory;

    protected virtual string RegionName => Region.SystemName;

    protected virtual Amazon.RegionEndpoint Region => TestEnvironment.Region;

    protected virtual Uri ServiceUri => TestEnvironment.SimulatorUrl;

    protected virtual bool IsSimulator => TestEnvironment.IsSimulatorConfigured;

    protected virtual InMemoryAwsBus Bus { get; } = new InMemoryAwsBus();

    /// <summary>
    /// A unique 12-digit AWS account id used for this test instance.
    /// When tests run against floci, this is supplied as the access key so floci
    /// gives each test its own isolated account.
    /// </summary>
    protected virtual string AccountId { get; } = GenerateAccountId();

    protected virtual TimeSpan Timeout => TimeSpan.FromSeconds(
        Debugger.IsAttached ? 300 : (TestEnvironment.UseFloci ? 30 : 10));

    protected virtual string UniqueName { get; } = $"{Guid.NewGuid():N}-integration-tests";

    private static string GenerateAccountId()
    {
        Span<byte> bytes = stackalloc byte[8];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        ulong value = BitConverter.ToUInt64(bytes) % 1_000_000_000_000UL;
        return value.ToString("D12", System.Globalization.CultureInfo.InvariantCulture);
    }

    protected IServiceCollection GivenJustSaying(LogLevel? levelOverride = null)
        => Given((_) => { }, levelOverride);

    protected IServiceCollection Given(
        Action<MessagingBusBuilder> configure,
        LogLevel? levelOverride = null)
        => Given((builder, _) => configure(builder), levelOverride);

    protected IServiceCollection Given(
        Action<MessagingBusBuilder, IServiceProvider> configure,
        LogLevel? levelOverride = null)
    {
        LogLevel logLevel = levelOverride ?? LogLevel.Debug;
        return new ServiceCollection()
            .AddLogging((p) => p
                .AddFakeLogging()
                .AddTextWriter(OutputHelper, o =>
                {
                    o.IncludeScopes = true;
                    o.Filter = (_, level) => level >= logLevel;
                }).SetMinimumLevel(logLevel))
            .AddJustSaying(
                (builder, serviceProvider) =>
                {
                    builder.Messaging((options) => options.WithRegion(RegionName))
                        .Client((options) =>
                        {
                            options.WithClientFactory(CreateClientFactory);
                        });
                    builder.Subscriptions(sub => sub.WithDefaults(x => x.WithDefaultConcurrencyLimit(10)));

                    configure(builder, serviceProvider);
                });
    }

    protected virtual IAwsClientFactory CreateClientFactory()
    {
        if (TestEnvironment.UseFloci)
        {
            var port = AspireFixture.ServicePort
                ?? throw new InvalidOperationException("USE_FLOCI is set but the floci container endpoint has not been allocated.");
            var serviceUri = new Uri($"http://localhost:{port}", UriKind.Absolute);
            return new FlociAwsClientFactory(serviceUri, AccountId, RegionName);
        }

        return new LocalAwsClientFactory(Bus);
    }

    // Normalize the region and account id inside ARNs so approval snapshots
    // match regardless of which backend the test ran against. The in-memory
    // bus produces "arn:aws:{service}:us-east-1:000000000000:..." while floci
    // produces ARNs with the test's random 12-digit account id and configured
    // region.
    private static readonly System.Text.RegularExpressions.Regex ArnRegex =
        new("(arn:aws:[a-z0-9-]+):[a-z0-9-]+:[0-9]{12}:",
            System.Text.RegularExpressions.RegexOptions.Compiled);

    protected static string ScrubArns(string value)
        => value is null ? null : ArnRegex.Replace(value, "$1:us-east-1:000000000000:");

    protected IHandlerAsync<T> CreateHandler<T>(TaskCompletionSource<object> completionSource, int expectedMessageCount = 1)
        where T : Message
    {
        IHandlerAsync<T> handler = Substitute.For<IHandlerAsync<T>>();

        var counter = 0;
        handler.Handle(Arg.Any<T>())
            .Returns(true)
            .AndDoes(x =>
            {
                if (Interlocked.Increment(ref counter) == expectedMessageCount)
                {
                    completionSource.TrySetResult(null);
                }
            });

        return handler;
    }

    protected async Task WhenAsync(
        IServiceCollection services,
        Func<IMessagePublisher, IMessagingBus, CancellationToken, Task> action)
        => await WhenAsync(services, async (p, b, _, c) => await action(p, b, c));

    protected async Task WhenAsync(
        IServiceCollection services,
        Func<IMessagePublisher, IMessagingBus, IServiceProvider, CancellationToken, Task> action)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        await RunActionWithTimeout(async cancellationToken =>
            await action(publisher, listener, serviceProvider, cancellationToken)
                .ConfigureAwait(false));
    }

    protected async Task WhenBatchAsync(
        IServiceCollection services,
        Func<IMessageBatchPublisher, IMessagingBus, CancellationToken, Task> action)
        => await WhenBatchAsync(services, async (p, b, _, c) => await action(p, b, c));

    protected async Task WhenBatchAsync(
        IServiceCollection services,
        Func<IMessageBatchPublisher, IMessagingBus, IServiceProvider, CancellationToken, Task> action)
    {
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        var publisher = serviceProvider.GetRequiredService<IMessageBatchPublisher>();
        IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

        await RunActionWithTimeout(async cancellationToken =>
            await action(publisher, listener, serviceProvider, cancellationToken)
                .ConfigureAwait(false));
    }

    protected async Task RunActionWithTimeout(Func<CancellationToken, Task> action)
    {
        // See https://speakerdeck.com/davidfowl/scaling-asp-dot-net-core-applications?slide=28
        using var cts = new CancellationTokenSource();
        var delayTask = Task.Delay(Timeout, cts.Token);
        var actionTask = action(cts.Token);

        var resultTask = await Task.WhenAny(actionTask, delayTask)
            .ConfigureAwait(false);

        if (resultTask == delayTask)
        {
            throw new TimeoutException(
                $"The tested action took longer than the timeout of {Timeout} to complete.");
        }
        else
        {
            cts.Cancel();
        }

        await actionTask;
    }

    protected async Task<string> GivenAnExistingTopic(string topicName, CancellationToken cancellationToken)
    {
        var client = CreateClientFactory().GetSnsClient(Region);

        var createTopicRequest = new CreateTopicRequest
        {
            Name = topicName,
        };
        var response = await client.CreateTopicAsync(createTopicRequest, cancellationToken);

        var policyDetails = new SnsPolicyDetails
        {
            SourceArn = response.TopicArn,
        };
        await SnsPolicy.SaveAsync(policyDetails, client);

        return response.TopicArn;
    }
}
