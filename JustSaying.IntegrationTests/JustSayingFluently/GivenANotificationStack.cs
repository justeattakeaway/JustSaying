using System;
using System.Diagnostics;
using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public abstract class GivenANotificationStack : AsyncBehaviourTest<IAmJustSayingFluently>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IAmJustSayingFluently ServiceBus;
        protected IMessageMonitor Monitoring;
        private Future<GenericMessage> _snsHandler;
        private Future<AnotherGenericMessage> _sqsHandler;
        private IPublishConfiguration _config = 
            new MessagingConfig
            {
                PublishFailureBackoffMilliseconds = 1,
                PublishFailureReAttempts = 3
            };

        protected void RegisterSnsHandler(Future<GenericMessage> handler)
        {
            _snsHandler = handler;
        }

        protected void RegisterSqsHandler(Future<AnotherGenericMessage> handler)
        {
            _sqsHandler = handler;
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
            const int TimeoutMillis = 1000;

            var snsHandler = Substitute.For<IHandlerAsync<GenericMessage>>();
            snsHandler.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x =>
                    {
                        var msg = (GenericMessage) x.Args()[0];
                        if (_snsHandler != null)
                        {
                            _snsHandler.Complete(msg).Wait(TimeoutMillis);
                        }
                    });

            var sqsHandler = Substitute.For<IHandlerAsync<AnotherGenericMessage>>();
            sqsHandler.When(x => x.Handle(Arg.Any<AnotherGenericMessage>()))
                    .Do(x =>
                    {
                        var msg = (AnotherGenericMessage)x.Args()[0];
                        if (_sqsHandler != null)
                        {
                            _sqsHandler.Complete(msg).Wait(TimeoutMillis);
                        }
                    });

            Monitoring = Substitute.For<IMessageMonitor>();

            ServiceBus = CreateMeABus.InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithMonitoring(Monitoring)

                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = _config.PublishFailureBackoffMilliseconds;
                    c.PublishFailureReAttempts = _config.PublishFailureReAttempts;
                })

                .WithSnsMessagePublisher<GenericMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueueNamed("queuename")
                .ConfigureSubscriptionWith(cf =>
                {
                    cf.MessageRetentionSeconds = 60;
                    cf.VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
                    cf.InstancePosition = 1;
                })
                .WithMessageHandler(snsHandler)

                .WithSqsMessagePublisher<AnotherGenericMessage>(configuration => { })
                .WithSqsPointToPointSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(sqsHandler);

            ServiceBus.StartListening();
            return ServiceBus;
        }

        public override void PostAssertTeardown()
        {
            base.PostAssertTeardown();
            _stopwatch.Stop();
            Teardown();
            Console.WriteLine($"The test took {_stopwatch.ElapsedMilliseconds/1000} seconds.");

            ServiceBus.StopListening();
        }
    }
}