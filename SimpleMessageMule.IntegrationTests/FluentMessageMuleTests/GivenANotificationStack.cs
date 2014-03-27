using System;
using System.Diagnostics;
using Amazon;
using JustEat.Simples.NotificationStack.AwsTools;
using JustEat.Simples.NotificationStack.Messaging.MessageHandling;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using SimpleMessageMule;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public abstract class GivenANotificationStack : BehaviourTest<IFluentMessageMule>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IFluentSubscription ServiceBus;
        protected IMessageMonitor Monitoring;
        private Future<GenericMessage> _handler;
        private  INotificationStackConfiguration _config = new MessagingConfig { PublishFailureBackoffMilliseconds = 1, PublishFailureReAttempts = 3};

        protected void RegisterHandler(Future<GenericMessage> handler)
        {
            _handler = handler;
        }

        protected void RegisterConfig(INotificationStackConfiguration config)
        {
            _config = config;
        }

        protected override void Given()
        {
            _stopwatch.Start();
        }

        protected override IFluentMessageMule CreateSystemUnderTest()
        {
            Monitoring = Substitute.For<IMessageMonitor>();
            ServiceBus = FluentMessagingMule.Register(c =>
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
                    cf.VisibilityTimeoutSeconds = NotificationStackConstants.DEFAULT_VISIBILITY_TIMEOUT;
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