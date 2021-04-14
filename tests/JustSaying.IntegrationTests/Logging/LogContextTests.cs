using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.IntegrationTests.Fluent;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.TestingFramework;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit.Abstractions;

namespace JustSaying.Logging
{
    public class LogContextTests : IntegrationTestBase
    {
        public LogContextTests(ITestOutputHelper outputHelper) : base(outputHelper)
        { }

        [AwsFact]
        public async Task PublishToTopicLogsShouldHaveContext()
        {
            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.WithLoopbackTopic<SimpleMessage>(UniqueName));

            var sp = services.BuildServiceProvider();

            var cts = new CancellationTokenSource();

            var publisher = sp.GetRequiredService<IMessagePublisher>();
            await publisher.StartAsync(cts.Token);

            var message = new SimpleMessage();
            await publisher.PublishAsync(message, cts.Token);

            var output = OutputHelper.Output;
            output.ShouldMatchApproved(o => o
                .SubFolder("Approvals")
                .WithScrubber(logMessage => ScrubLogs(logMessage, message.Id.ToString())));

            cts.Cancel();
        }

        [AwsFact]
        public async Task PublishToQueueLogsShouldHaveContext()
        {
            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName));

            var sp = services.BuildServiceProvider();

            var cts = new CancellationTokenSource();

            var publisher = sp.GetRequiredService<IMessagePublisher>();
            await publisher.StartAsync(cts.Token);

            var message = new SimpleMessage();
            await publisher.PublishAsync(message, cts.Token);

            var output = OutputHelper.Output;
            output.ShouldMatchApproved(o => o
                .SubFolder("Approvals")
                .WithScrubber(logMessage => ScrubLogs(logMessage, message.Id.ToString())));

            cts.Cancel();
        }

        [AwsFact]
        public async Task HandleMessageFromQueueLogs_ShouldHaveContext()
        {
            var handler = new InspectableHandler<SimpleMessage>();

            var services = GivenJustSaying()
                .ConfigureJustSaying(
                    (builder) => builder.WithLoopbackQueue<SimpleMessage>(UniqueName))
                .AddSingleton<IHandlerAsync<SimpleMessage>>(handler);

            var sp = services.BuildServiceProvider();

            var cts = new CancellationTokenSource();

            var publisher = sp.GetRequiredService<IMessagePublisher>();
            await publisher.StartAsync(cts.Token);
            await sp.GetRequiredService<IMessagingBus>().StartAsync(cts.Token);

            var message = new SimpleMessage();
            await publisher .PublishAsync(message, cts.Token);

            await Patiently.AssertThatAsync(OutputHelper,
                () =>
                {
                    handler.ReceivedMessages
                        .ShouldHaveSingleItem()
                        .Id.ShouldBe(message.Id);
                });

            var output = ((TestOutputHelper) OutputHelper).Output;
            output.ShouldMatchApproved(o => o
                .SubFolder("Approvals")
                .WithScrubber(logMessage => ScrubLogs(logMessage, message.Id.ToString())));

            cts.Cancel();
        }

        private string ScrubLogs(string message, string messageId)
        {
            message = message.Replace(messageId, "{MessageId}");
            message = message.Replace(UniqueName, "{TestDiscriminator}");

            message = Regex.Replace(message, @"AwsRequestId: .{8}-.{4}-.{4}-.{4}-.{12}", "AwsRequestId: {AwsRequestId}");
            message = Regex.Replace(message, @"(\d{4})-(\d{2})-(\d{2}) (\d{2}):(\d{2}):(\d{2})Z", "{DateTime}");
            message = Regex.Replace(message, @"(\d+).(\d+) ms", "{Duration}");
            message = Regex.Replace(message, @"in (\d+)ms.", "{Duration}");
            return message;
        }
    }
}
