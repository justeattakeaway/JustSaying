using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    //Todo: Must rewrite using a loopback transport.
    [Collection(GlobalSetup.CollectionName)]
    public class WhenThrottlingIsEnabledALongRunningHandler : IDisposable
    {
        private readonly IHandlerAsync<GenericMessage> _handler = Substitute.For<IHandlerAsync<GenericMessage>>();
        private IAmJustSayingFluently _publisher;
        private readonly Dictionary<int, Guid> _ids = new Dictionary<int, Guid>();
        private readonly Dictionary<int, GenericMessage> _messages = new Dictionary<int, GenericMessage>();

        public WhenThrottlingIsEnabledALongRunningHandler()
        {
            Enumerable.Range(1, 100).ToList().ForEach(i =>
            {
                _ids.Add(i, Guid.NewGuid());
                _messages.Add(i, new GenericMessage() { Id = _ids[i] });

                //First handler takes ages all the others take 100 ms
                SetUpHandler(_ids[i], i, wait: i == 1 ? 3600000 : 100);
            });
            
            var publisher = CreateMeABus.WithLogging(new LoggerFactory())
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(Substitute.For<IMessageMonitor>())
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = 1;
                })
                .WithSnsMessagePublisher<GenericMessage>()
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
            _handler.When(x => x.Handle(Arg.Is<GenericMessage>(y => y.Id == guid))).Do(t =>
            {
                Console.WriteLine($"Running task {number}");
                Thread.Sleep(wait);
            });
        }

        [Fact]
        public async Task ThenItGetsHandled()
        {
            //Publish the message with a long running handler
            await _publisher.PublishAsync(_messages[1]);

            //Give some time to AWS to schedule the first long running message
            await Task.Delay(TimeSpan.FromSeconds(2));

            //publish the rest of the messages except the last one.
            Enumerable.Range(2, 98).ToList().ForEach(i => _publisher.PublishAsync(_messages[i]).Wait());

            //publish the last message after a couple of seconds to guaranty it was scheduled after all the rest
            await Task.Delay(TimeSpan.FromSeconds(2));
            await _publisher.PublishAsync(_messages[100]);

            //Wait for a reasonble time before asserting whether the last message has been scheduled.
            //There are 100 messages and all except one takes 100 ms. Therefore, 20 seconds is sufficiently long.
            Thread.Sleep(20000);

            Received.InOrder(async () => await _handler.Handle(Arg.Is<GenericMessage>(x => x.Id == _ids[100])));
        }

        public void Dispose()
        {
            _publisher.StopListening();
            _publisher = null;
        }
    }
}
