using System;
using System.Diagnostics;
using Amazon;
using JustBehave;
using JustSaying.AwsTools;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.Monitoring;
using JustSaying.TestingFramework;
using NSubstitute;

namespace JustSaying.IntegrationTests.JustSayingFluently.MultiRegion.WithSqsTopicSubscriber
{
    public abstract class WhenSubscribingToTwoRegions : BehaviourTest<IAmJustSayingFluently>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IAmJustSayingFluently ServiceBus;
        protected IMessageMonitor Monitoring;
        protected Future<GenericMessage> SnsHandler;
        protected IPublishConfiguration Config = new MessagingConfig {PublishFailureBackoffMilliseconds = 1, PublishFailureReAttempts = 3};
        protected Future<GenericMessage> Handler;
        

        protected void RegisterSnsHandler(Future<GenericMessage> handler)
        {
            SnsHandler = handler;
        }

        protected void RegisterConfig(IPublishConfiguration config)
        {
            Config = config;
        }

        protected override void Given()
        {
            _stopwatch.Start();
            Handler = new Future<GenericMessage>();
            RegisterSnsHandler(Handler);
        }

        protected override IAmJustSayingFluently CreateSystemUnderTest()
        {
            var snsHandler = Substitute.For<IHandler<GenericMessage>>();
            snsHandler.When(x => x.Handle(Arg.Any<GenericMessage>()))
                .Do(x => SnsHandler.Complete((GenericMessage)x.Args()[0]));

            Monitoring = Substitute.For<IMessageMonitor>();

            ServiceBus = CreateMeABus
                .InRegion(RegionEndpoint.EUWest1.SystemName)
                .WithFailoverRegion(RegionEndpoint.USEast1.SystemName)
                .WithActiveRegion(() => RegionEndpoint.USEast1.SystemName)
                .WithMonitoring(Monitoring)
                .WithSqsTopicSubscriber()
                .IntoQueue("queuename3")
                .ConfigureSubscriptionWith(cf =>
                {
                    cf.MessageRetentionSeconds = 60;
                    cf.VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
                    cf.InstancePosition = 1;
                })
                .WithMessageHandler(snsHandler);

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
        

        protected abstract override void When();

    }
}