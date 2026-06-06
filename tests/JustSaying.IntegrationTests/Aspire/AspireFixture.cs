using System.Reflection;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TUnit.Core.Interfaces;

namespace JustSaying.IntegrationTests.Aspire;

/// <summary>
/// A per-test-session fixture that, when floci mode is enabled (<c>USE_FLOCI=1</c>),
/// spins up the floci container via the Aspire test host and exposes its endpoint.
/// When floci mode is disabled this is a no-op and the tests use the in-memory
/// <c>LocalSqsSnsMessaging</c> bus, so the container is only started on demand.
/// </summary>
public sealed class AspireFixture : IAsyncInitializer, IAsyncDisposable
{
    private static int _suppressorInstalled;

    private DistributedApplication _app;
    private IDistributedApplicationTestingBuilder _builder;

    /// <summary>
    /// The dynamically-allocated host port for the floci container, or <c>null</c>
    /// when floci mode is not enabled.
    /// </summary>
    public int? ServicePort => _app?.GetEndpoint("floci").Port;

    public async Task InitializeAsync()
    {
        if (!TestEnvironment.UseFloci)
        {
            // In-memory mode: nothing to start.
            return;
        }

        SuppressAspireShutdownNoise();

#pragma warning disable CA1849
        // ReSharper disable once MethodHasAsyncOverload
        _builder = DistributedApplicationTestingBuilder.Create();
#pragma warning restore CA1849

        _builder.AddFloci();

        // Silence the Aspire host's own logging so it does not pollute test output.
        _builder.Services.Add(ServiceDescriptor.Singleton<ILoggerFactory>(NullLoggerFactory.Instance));
        _builder.Services.Add(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(NullLogger<>)));

        _app = await _builder.BuildAsync();

        await _app.StartAsync();

        await _app.ResourceNotifications.WaitForResourceHealthyAsync("floci");
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }

        if (_builder is not null)
        {
            await _builder.DisposeAsync();
        }
    }

    /// <summary>
    /// As the Aspire test host drives the floci container through its lifecycle it tears down
    /// and recreates DCP resource-watch streams. Each aborted watch surfaces a benign
    /// "The request was aborted." <see cref="IOException"/> from the underlying Kubernetes
    /// watch client which, because nothing awaits the background read task, the GC reports as
    /// an unobserved task exception. The test platform logs every such exception as a warning,
    /// which spams the output even though the run is healthy.
    /// </summary>
    /// <remarks>
    /// <see cref="TaskScheduler.UnobservedTaskException"/> is multicast and the platform's
    /// handler logs unconditionally, so calling <c>SetObserved()</c> from an additional
    /// subscriber does not suppress it. Instead we swap the event with a filter that drops only
    /// these benign Aspire/DCP exceptions and forwards everything else to the original handlers,
    /// so genuine unobserved exceptions are still reported. This only runs in floci mode and is
    /// guarded so that, if the internals ever change, we simply fall back to the original noise.
    /// </remarks>
    private static void SuppressAspireShutdownNoise()
    {
        if (Interlocked.Exchange(ref _suppressorInstalled, 1) == 1)
        {
            return;
        }

        try
        {
            var field = typeof(TaskScheduler).GetField(
                nameof(TaskScheduler.UnobservedTaskException),
                BindingFlags.Static | BindingFlags.NonPublic);

            if (field?.GetValue(null) is not EventHandler<UnobservedTaskExceptionEventArgs> original)
            {
                return;
            }

            field.SetValue(null, (EventHandler<UnobservedTaskExceptionEventArgs>)((sender, e) =>
            {
                if (IsBenignAspireShutdown(e.Exception))
                {
                    e.SetObserved();
                    return;
                }

                original(sender, e);
            }));
        }
#pragma warning disable CA1031
        catch (Exception)
#pragma warning restore CA1031
        {
            // Best-effort cosmetic cleanup; never let it affect the test run.
        }
    }

    private static bool IsBenignAspireShutdown(AggregateException exception)
    {
        var inner = exception.Flatten().InnerExceptions;
        return inner.Count > 0 && inner.All(static ex =>
            ex is IOException
            && (ex.Message.Contains("The request was aborted", StringComparison.Ordinal)
                || (ex.StackTrace?.Contains("k8s.", StringComparison.Ordinal) ?? false)));
    }
}
