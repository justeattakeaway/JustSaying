using System;
using System.Diagnostics;
using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.IntegrationTests.TestHandlers;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public abstract class GivenANotificationStack : XAsyncBehaviourTest<IAmJustSayingFluently>
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        private Future<SimpleMessage> _snsHandler;
        private Future<AnotherSimpleMessage> _sqsHandler;
        private IPublishConfiguration _config =
            new MessagingConfig
            {
                PublishFailureBackoffMilliseconds = 1,
                PublishFailureReAttempts = 3
            };

        protected IAmJustSayingFluently ServiceBus { get; set; }

        protected IMessageMonitor Monitoring { get; set; }

        protected ILoggerFactory LoggerFactory => TestFixture.LoggerFactory;

        protected RegionEndpoint Region =>TestFixture.Region;

        private JustSayingFixture TestFixture { get; } = new JustSayingFixture();

        protected void RegisterSnsHandler(Future<SimpleMessage> handler)
        {
            _snsHandler = handler;
        }

        protected void RegisterSqsHandler(Future<AnotherSimpleMessage> handler)
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

            var snsHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            snsHandler.When(x => x.Handle(Arg.Any<SimpleMessage>()))
                    .Do(x =>
                    {
                        var msg = (SimpleMessage) x.Args()[0];
                        _snsHandler?.Complete(msg).Wait(TimeoutMillis);
                    });

            var sqsHandler = Substitute.For<IHandlerAsync<AnotherSimpleMessage>>();
            sqsHandler.When(x => x.Handle(Arg.Any<AnotherSimpleMessage>()))
                    .Do(x =>
                    {
                        var msg = (AnotherSimpleMessage)x.Args()[0];
                        _sqsHandler?.Complete(msg).Wait(TimeoutMillis);
                    });

            Monitoring = Substitute.For<IMessageMonitor>();

            ServiceBus = TestFixture.Builder()
                .WithMonitoring(Monitoring)
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoffMilliseconds = _config.PublishFailureBackoffMilliseconds;
                    c.PublishFailureReAttempts = _config.PublishFailureReAttempts;
                })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(TestFixture.UniqueName)
                .ConfigureSubscriptionWith(cf =>
                {
                    cf.MessageRetentionSeconds = 60;
                    cf.VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
                    cf.InstancePosition = 1;
                })
                .WithMessageHandler(snsHandler)
                .WithSqsMessagePublisher<AnotherSimpleMessage>(configuration => { })
                .WithSqsPointToPointSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(sqsHandler);

            ServiceBus.StartListening();

            return ServiceBus;
        }

        protected override void PostAssertTeardown()
        {
            base.PostAssertTeardown();

            _stopwatch.Stop();
            Teardown();

            // TODO ITestOutputHelper
            Console.WriteLine($"The test took {_stopwatch.ElapsedMilliseconds / 1000} seconds.");

            ServiceBus.StopListening();
        }
    }
}
