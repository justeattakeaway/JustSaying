using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    [Collection(GlobalSetup.CollectionName)]
    public class WhenThrottlingIsEnabledALongRunningHandler : IDisposable
    {
        private readonly ITestOutputHelper _outputHelper;
        private readonly IHandlerAsync<SimpleMessage> _handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
        private readonly Dictionary<int, Guid> _ids = new Dictionary<int, Guid>();
        private readonly Dictionary<int, SimpleMessage> _messages = new Dictionary<int, SimpleMessage>();
        private IAmJustSayingFluently _publisher;
        private CancellationTokenSource _publisherCts;

        public WhenThrottlingIsEnabledALongRunningHandler(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            // First handler takes ages all the others take 100 ms
            int waitOthers = 100;
            int waitOne = TestEnvironment.IsSimulatorConfigured ? waitOthers : 3_600_000;

            for (int i = 1; i <= 100; i++)
            {
                _ids.Add(i, Guid.NewGuid());
                _messages.Add(i, new SimpleMessage() { Id = _ids[i] });


                SetUpHandler(_ids[i], i, wait: i == 1 ? waitOne : waitOthers);
            }

            var fixture = new JustSayingFixture(_outputHelper);

            _publisher = fixture.Builder()
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoff = TimeSpan.FromMilliseconds(1);
                })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(fixture.UniqueName)
                .ConfigureSubscriptionWith(
                    cfg =>
                    {
                        cfg.InstancePosition = 1;
                        cfg.MaxAllowedMessagesInFlight = 25;
                    })
                .WithMessageHandler(_handler);

            _publisherCts = new CancellationTokenSource();
            _publisher.StartListening(_publisherCts.Token);
        }

        private void SetUpHandler(Guid guid, int number, int wait)
        {
            _handler
                .When(x => x.Handle(Arg.Is<SimpleMessage>(y => y.Id == guid)))
                .Do(t =>
                {
                    _outputHelper.WriteLine($"Running task {number}");
                    Thread.Sleep(wait);
                });
        }

        [AwsFact]
        public async Task ThenItGetsHandled()
        {
            TimeSpan baseSleep = TestEnvironment.IsSimulatorConfigured ? TimeSpan.FromMilliseconds(100) : TimeSpan.FromSeconds(2);

            // Publish the message with a long running handler
            await _publisher.PublishAsync(_messages[1]);

            // Give some time to AWS to schedule the first long running message
            await Task.Delay(baseSleep);

            // Publish the rest of the messages except the last one.
            for (int i = 2; i <= 98; i++)
            {
                await _publisher.PublishAsync(_messages[i]);
            }

            // Publish the last message after a couple of seconds to guaranty it was scheduled after all the rest
            await Task.Delay(baseSleep);
            await _publisher.PublishAsync(_messages[100]);

            // Wait for a reasonble time before asserting whether the last message has been scheduled.
            await Task.Delay(baseSleep * 10);

            Received.InOrder(() => _handler.Handle(Arg.Is<SimpleMessage>(x => x.Id == _ids[100])));
        }

        public void Dispose()
        {
            _publisherCts.Cancel();
            _publisher = null;
        }
    }
}
