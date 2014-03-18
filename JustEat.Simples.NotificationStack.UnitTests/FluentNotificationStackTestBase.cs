using System;
using System.Reflection;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Simples.NotificationStack.Stack.Amazon;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests
{
    public abstract class FluentNotificationStackTestBase : BehaviourTest<FluentNotificationStack>
    {
        protected INotificationStackConfiguration Configuration;
        protected INotificationStack NotificationStack;
        protected IMessageMonitor Monitor = Substitute.For<IMessageMonitor>();
        protected override void Given()
        {
            throw new NotImplementedException();
        }

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            if (Configuration == null)
            {
                Configuration = new MessagingConfig
                {
                    Component = "OrderEngine",
                    Tenant = "LosAlamos",
                    Environment = "unitest"
                };
            }

            var fns = FluentNotificationStack.Register(x =>
            {
                x.Component = Configuration.Component;
                x.Environment = Configuration.Environment;
                x.PublishFailureBackoffMilliseconds = Configuration.PublishFailureBackoffMilliseconds;
                x.PublishFailureReAttempts = Configuration.PublishFailureReAttempts;
                x.Region = Configuration.Region;
                x.Tenant = Configuration.Tenant;
            }).WithMonitoring(Monitor) as FluentNotificationStack;


            ConfigureNotificationStackMock(fns);

            ConfigureAmazonQueueCreator(fns);

            return fns;
        }

        private void ConfigureNotificationStackMock(FluentNotificationStack fns)
        {
            NotificationStack = Substitute.For<INotificationStack>();

            var notificationStackField = fns.GetType().GetField("_stack", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (JustEat.Simples.NotificationStack.Stack.NotificationStack)notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        private void ConfigureAmazonQueueCreator(FluentNotificationStack fns)
        {
            fns.GetType()
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fns, Substitute.For<IVerifyAmazonQueues>());
        }

        protected override void When()
        {
            throw new NotImplementedException();
        }
    }
}