using System;
using System.Threading.Tasks;
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

        protected virtual string Region => "eu-west-1";

        protected virtual Uri ServiceUri => TestEnvironment.SimulatorUrl;

        protected virtual string UniqueName { get; } = $"{DateTime.UtcNow.Ticks}-integration-tests";

        protected IServiceCollection Given(Action<MessagingBusBuilder> configure)
            => Given((builder, _) => configure(builder));

        protected IServiceCollection Given(Action<MessagingBusBuilder, IServiceProvider> configure)
        {
            return new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper))
                .AddJustSaying(
                    (builder, serviceProvider) =>
                    {
                        builder.Messaging((options) => options.WithRegion(Region))
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
    }
}
