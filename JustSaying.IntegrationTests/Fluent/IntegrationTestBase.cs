using System;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.Fluent
{
    public abstract class IntegrationTestBase
    {
        protected IntegrationTestBase(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        protected virtual string AccessKeyId { get; } = "accessKeyId";

        protected virtual string SecretAccessKey { get; } = "secretAccessKey";

        protected virtual string SessionToken { get; } = "token";

        protected ITestOutputHelper OutputHelper { get; }

        protected virtual string RegionName => Region.SystemName;

        protected virtual Amazon.RegionEndpoint Region => TestEnvironment.Region;

        protected virtual Uri ServiceUri => TestEnvironment.SimulatorUrl;

        protected virtual bool IsSimulator => TestEnvironment.IsSimulatorConfigured;

        protected virtual TimeSpan Timeout => TimeSpan.FromSeconds(20);

        protected virtual string UniqueName { get; } = $"{DateTime.UtcNow.Ticks}-integration-tests";

        protected IServiceCollection GivenJustSaying()
            => Given((_) => { });

        protected IServiceCollection Given(Action<MessagingBusBuilder> configure)
            => Given((builder, _) => configure(builder));

        protected IServiceCollection Given(Action<MessagingBusBuilder, IServiceProvider> configure)
        {
            return new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying(
                    (builder, serviceProvider) =>
                    {
                        builder.Messaging((options) => options.WithRegion(RegionName))
                               .Client((options) =>
                                {
                                    options.WithSessionCredentials(AccessKeyId, SecretAccessKey, SessionToken)
                                           .WithServiceUri(ServiceUri);
                                });

                        configure(builder, serviceProvider);
                    });
        }

        protected IHandlerAsync<T> CreateHandler<T>(TaskCompletionSource<object> completionSource)
            where T : Message
        {
            IHandlerAsync<T> handler = Substitute.For<IHandlerAsync<T>>();

            handler.Handle(Arg.Any<T>())
                   .Returns(true)
                   .AndDoes((_) => completionSource.SetResult(null));

            return handler;
        }

        protected async Task WhenAsync(IServiceCollection services, Func<IMessagePublisher, IMessagingBus, CancellationToken, Task> action)
        {
            IServiceProvider serviceProvider = services.BuildServiceProvider();

            IMessagePublisher publisher = serviceProvider.GetRequiredService<IMessagePublisher>();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            using (var source = new CancellationTokenSource(Timeout))
            {
                try
                {
                    var delayTask = Task.Delay(Timeout, source.Token);
                    var actionTask = action(publisher, listener, source.Token);

                    await Task.WhenAny(actionTask, delayTask).ConfigureAwait(false);

                    source.Token.ThrowIfCancellationRequested();

                    if (actionTask.IsFaulted)
                    {
                        await actionTask;
                    }
                }
                finally
                {
                    source.Cancel();
                }
            }
        }
    }
}
