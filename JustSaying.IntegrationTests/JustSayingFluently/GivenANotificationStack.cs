using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
                PublishFailureBackoff = TimeSpan.FromMilliseconds(1),
                PublishFailureReAttempts = 3
            };

        private CancellationTokenSource _subscriberCts;

        protected IAmJustSayingFluently ServiceBus { get; set; }

        protected IMessageMonitor Monitoring { get; set; }

        protected ILoggerFactory LoggerFactory => TestFixture.LoggerFactory;

        protected RegionEndpoint Region => TestFixture.Region;

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

        protected override Task Given()
        {
            _stopwatch.Start();
            return Task.CompletedTask;
        }

        protected override Task<IAmJustSayingFluently> CreateSystemUnderTestAsync()
        {
            var timeout = TimeSpan.FromSeconds(1);

            var snsHandler = Substitute.For<IHandlerAsync<SimpleMessage>>();
            snsHandler.Handle(Arg.Any<SimpleMessage>()).Returns(async x =>
            {
                var msg = (SimpleMessage) x.Args()[0];
                await Tasks.WaitWithTimeoutAsync(_snsHandler?.Complete(msg), timeout);
            });

            var sqsHandler = Substitute.For<IHandlerAsync<AnotherSimpleMessage>>();
            sqsHandler.Handle(Arg.Any<AnotherSimpleMessage>())
                .Returns(async x =>
                {
                    var msg = (AnotherSimpleMessage)x.Args()[0];
                    await Tasks.WaitWithTimeoutAsync(_sqsHandler?.Complete(msg), timeout);
                });

            Monitoring = Substitute.For<IMessageMonitor>();

            ServiceBus = TestFixture.Builder()
                .WithMonitoring(Monitoring)
                .ConfigurePublisherWith(c =>
                {
                    c.PublishFailureBackoff = _config.PublishFailureBackoff;
                    c.PublishFailureReAttempts = _config.PublishFailureReAttempts;
                })
                .WithSnsMessagePublisher<SimpleMessage>()
                .WithSqsTopicSubscriber()
                .IntoQueue(TestFixture.UniqueName)
                .ConfigureSubscriptionWith(cf =>
                {
                    cf.MessageRetention = TimeSpan.FromSeconds(60);
                    cf.VisibilityTimeout = JustSayingConstants.DefaultVisibilityTimeout;
                    cf.InstancePosition = 1;
                })
                .WithMessageHandler(snsHandler)
                .WithSqsMessagePublisher<AnotherSimpleMessage>(configuration => { })
                .WithSqsPointToPointSubscriber()
                .IntoDefaultQueue()
                .WithMessageHandler(sqsHandler);

            _subscriberCts = new CancellationTokenSource();
            ServiceBus.StartListening(_subscriberCts.Token);

            return Task.FromResult(ServiceBus);
        }

        protected override Task PostAssertTeardownAsync()
        {
            _stopwatch.Stop();
            Teardown();

            // TODO ITestOutputHelper
            Console.WriteLine($"The test took {_stopwatch.ElapsedMilliseconds / 1000} seconds.");

            _subscriberCts.Cancel();
            return Task.CompletedTask;
        }
    }
}
