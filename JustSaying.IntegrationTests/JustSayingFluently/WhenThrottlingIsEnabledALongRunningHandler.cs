using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
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
        private readonly IHandlerAsync<SimpleMessage> _handler = Substitute.For<IHandlerAsync<SimpleMessage>>();
        private IAmJustSayingFluently _publisher;
        private readonly Dictionary<int, Guid> _ids = new Dictionary<int, Guid>();
        private readonly Dictionary<int, SimpleMessage> _messages = new Dictionary<int, SimpleMessage>();
        private ITestOutputHelper _outputHelper;

        public WhenThrottlingIsEnabledALongRunningHandler(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;

            for (int i = 1; i <= 100; i++)
            {
                _ids.Add(i, Guid.NewGuid());
                _messages.Add(i, new SimpleMessage() { Id = _ids[i] });

                // First handler takes ages all the others take 100 ms
                SetUpHandler(_ids[i], i, wait: i == 1 ? 3600000 : 100);
            }
            
            var publisher = CreateMeABus.WithLogging(_outputHelper.AsLoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = 1;
                })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber().IntoQueue("queuename").ConfigureSubscriptionWith(
                    cfg =>
                    {
                        cfg.InstancePosition = 1;
                        cfg.MaxAllowedMessagesInFlight = 25;
                    })
                .WithMessageHandler(_handler);

            publisher.StartListening();
            _publisher = publisher;
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

        // TODO These sleeps could be reduced/removed when using local AWS simulator
        [Fact]
        public async Task ThenItGetsHandled()
        {
            // Publish the message with a long running handler
            await _publisher.PublishAsync(_messages[1]);

            // Give some time to AWS to schedule the first long running message
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Publish the rest of the messages except the last one.
            Enumerable.Range(2, 98).ToList().ForEach(i => _publisher.PublishAsync(_messages[i]).Wait());

            // Publish the last message after a couple of seconds to guaranty it was scheduled after all the rest
            await Task.Delay(TimeSpan.FromSeconds(2));
            await _publisher.PublishAsync(_messages[100]);

            // Wait for a reasonble time before asserting whether the last message has been scheduled.
            // There are 100 messages and all except one takes 100 ms. Therefore, 20 seconds is sufficiently long.
            await Task.Delay(TimeSpan.FromSeconds(20));

            Received.InOrder(() => _handler.Handle(Arg.Is<SimpleMessage>(x => x.Id == _ids[100])));
        }

        public void Dispose()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}
