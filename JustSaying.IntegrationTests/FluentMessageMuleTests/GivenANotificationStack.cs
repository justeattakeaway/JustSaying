using System;
using System.Diagnostics;
using Amazon;
using JustEat.Testing;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.Tests.MessageStubs;
using NSubstitute;

namespace JustSaying.IntegrationTests.FluentMessageMuleTests
{
    public abstract class GivenANotificationStack : BehaviourTest<IAmJustSayingFluently>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IFluentSubscription ServiceBus;
        protected IMessageMonitor Monitoring;
        private Future<GenericMessage> _handler;
        private  IPublishConfiguration _config = new MessagingConfig { PublishFailureBackoffMilliseconds = 1, PublishFailureReAttempts = 3};

        protected void RegisterHandler(Future<GenericMessage> handler)
        {
            _handler = handler;
        }

        protected void RegisterConfig(IPublishConfiguration config)
        {
            _config = config;
        }

        protected override void Given()
        {
            _stopwatch.Start();
        }

        protected override IAmJustSayingFluently CreateSystemUnderTest()
        {
            Monitoring = Substitute.For<IMessageMonitor>();
            ServiceBus = CreateMe.ABus(c =>
            {
                c.PublishFailureBackoffMilliseconds = _config.PublishFailureBackoffMilliseconds;
                c.PublishFailureReAttempts = _config.PublishFailureReAttempts;
                c.Region = RegionEndpoint.EUWest1.SystemName;
            })
                .WithMonitoring(Monitoring)
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber(cf =>
                {
                    cf.Topic = "CustomerCommunication";
                    cf.MessageRetentionSeconds = 60;
                    cf.VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
                    cf.InstancePosition = 1;
                });
            
            var handler = Substitute.For<IHandler<GenericMessage>>();
            handler.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x => _handler.Complete((GenericMessage)x.Args()[0]));

            ServiceBus.WithMessageHandler(handler);
            ServiceBus.StartListening();
            return ServiceBus;
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            _stopwatch.Stop();
            base.Teardown();
            Console.WriteLine("The test took {0} seconds.", _stopwatch.ElapsedMilliseconds / 1000);

            ServiceBus.StopListening();
        }
    }
}