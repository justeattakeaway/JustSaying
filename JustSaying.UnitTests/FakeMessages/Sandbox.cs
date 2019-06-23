using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using JustEat.HttpClientInterception;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.UnitTests.FakeMessages
{
    public partial class Sandbox
    {
        public Sandbox(ITestOutputHelper outputHelper)
        {
            OutputHelper = outputHelper;
        }

        private ITestOutputHelper OutputHelper { get; }

        [Fact]
        public async Task Can_Receive_Messages_From_Fake_Message_Pump()
        {
            // Arrange
            string queueName = Guid.NewGuid().ToString();
            string regionName = "eu-west-1";

            var credentials = new BasicAWSCredentials("accessKey", "secretKey");
            var interceptionOptions = new HttpClientInterceptorOptions().ThrowsOnMissingRegistration();

            var clientFactory = new InterceptingAwsClientFactory(credentials, interceptionOptions);
            var store = new SqsMessageStore(queueName, regionName, interceptionOptions);

            var services = new ServiceCollection()
                .AddLogging((p) => p.AddXUnit(OutputHelper).SetMinimumLevel(LogLevel.Information))
                .AddSingleton(OutputHelper)
                .AddJustSayingHandler<NumberMessage, NumberHandler>()
                .AddJustSaying(
                    (builder) =>
                    {
                        builder.Messaging((config) => config.WithRegion(regionName))
                               .Client((client) => client.WithClientFactory(() => clientFactory))
                               .Subscriptions((subscription) => subscription.ForQueue<NumberMessage>(queueName));
                    });

            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IMessagingBus listener = serviceProvider.GetRequiredService<IMessagingBus>();

            var tcs = new TaskCompletionSource<bool>();
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

            cts.Token.Register(() => tcs.TrySetResult(true));

            _ = Task.Run(async () =>
                {
                    int count = 1;
                    var delay = TimeSpan.FromSeconds(0.5);

                    while (!cts.IsCancellationRequested)
                    {
                        store.Add(new NumberMessage() { Number = count++ });
                        await Task.Delay(delay);
                    }
                });

            // Act
            listener.Start(cts.Token);

            await tcs.Task;
        }
    }
}
