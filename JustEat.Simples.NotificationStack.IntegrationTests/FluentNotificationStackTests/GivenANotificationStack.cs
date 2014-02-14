using System;
using System.Diagnostics;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using NUnit.Framework;
using Tests.MessageStubs;

namespace NotificationStack.IntegrationTests.FluentNotificationStackTests
{
    public abstract class GivenANotificationStack : BehaviourTest<IFluentNotificationStack>
    {
        readonly Stopwatch _stopwatch = new Stopwatch();
        protected IFluentSubscription ServiceBus;
        protected IMessageMonitor Monitoring;

        protected override void Given()
        {
            _stopwatch.Start();
        }

        protected override IFluentNotificationStack CreateSystemUnderTest()
        {
            Monitoring = Substitute.For<IMessageMonitor>();
            ServiceBus =  JustEat.Simples.NotificationStack.Stack.FluentNotificationStack.Register(c =>
            {
                c.Component = "TestHarnessHandling";
                c.Tenant = "Wherever";
                c.Environment = "integration";
                c.PublishFailureBackoffMilliseconds = 1;
                c.PublishFailureReAttempts = 3;
            })
                .WithMonitoring(Monitoring)
                .WithSnsMessagePublisher<GenericMessage>("CustomerCommunication")
                .WithSqsTopicSubscriber("CustomerCommunication", 60, instancePosition: 1);
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