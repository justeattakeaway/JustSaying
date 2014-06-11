using System;
using System.Diagnostics;
using JustSaying.AwsTools;
using JustSaying.Stack;
using JustEat.Testing;
using JustSaying;
using JustSaying.Messaging.Monitoring;
using JustSaying.Tests.MessageStubs;
using NSubstitute;
using NUnit.Framework;
using JustSaying.Messaging.MessageHandling;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public abstract class GivenANotificationStack : BehaviourTest<IFluentSubscription>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IHaveFulfilledSubscriptionRequirements ServiceBus;
        protected IMessageMonitor Monitoring;
        private Future<GenericMessage> _handler;
        private  INotificationStackConfiguration _config = new JustSaying.Stack.MessagingConfig { Component = "TestHarnessHandling", Tenant = "Wherever", Environment = "integration", PublishFailureBackoffMilliseconds = 1, PublishFailureReAttempts = 3};

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

        protected override IFluentSubscription CreateSystemUnderTest()
        {
            Monitoring = Substitute.For<IMessageMonitor>();
            
            var handler = Substitute.For<IHandler<GenericMessage>>();
            handler.When(x => x.Handle(Arg.Any<GenericMessage>()))
                    .Do(x => _handler.Complete((GenericMessage)x.Args()[0]));
            ServiceBus = FluentNotificationStack.Register(c =>
            {
                c.Component = _config.Component;
                c.Tenant = _config.Tenant;
                c.Environment = _config.Environment;
                c.PublishFailureBackoffMilliseconds = _config.PublishFailureBackoffMilliseconds;
                c.PublishFailureReAttempts = _config.PublishFailureReAttempts;
            })
                .WithMonitoring(Monitoring)
                .ConfigurePublisherWith(x => {})
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication")
                .IntoQueue("CustomerCommunication")
                .ConfigureSubscriptionWith(cf =>
                {

                    cf.MessageRetentionSeconds = 60;
                    cf.VisibilityTimeoutSeconds = JustSayingConstants.DEFAULT_VISIBILITY_TIMEOUT;
                    cf.InstancePosition = 1;
                }).WithMessageHandler(handler);

            ServiceBus.StartListening();
            return ServiceBus;
        }

        [TearDown]
        protected virtual void Teardown()
        {
            _stopwatch.Stop();
            base.Teardown();
            Console.WriteLine("The test took {0} seconds.", _stopwatch.ElapsedMilliseconds / 1000);

            ServiceBus.StopListening();
        }
    }
}