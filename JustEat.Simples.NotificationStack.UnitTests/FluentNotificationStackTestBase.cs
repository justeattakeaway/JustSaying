using System;
using System.Reflection;
using JustEat.Simples.NotificationStack.Messaging.Monitoring;
using JustEat.Simples.NotificationStack.AwsTools.QueueCreation;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;
using SimpleMessageMule;
using INotificationStackConfiguration = JustEat.Simples.NotificationStack.Stack.INotificationStackConfiguration;
using MessagingConfig = JustEat.Simples.NotificationStack.Stack.MessagingConfig;

namespace Stack.UnitTests
{
    public abstract class FluentNotificationStackTestBase : BehaviourTest<FluentNotificationStack>
    {
        protected INotificationStackConfiguration Configuration;
        protected INotificationStack NotificationStack;
        protected IMessageMonitor Monitor = Substitute.For<IMessageMonitor>();
        protected string Component = "OrderEngine";
        protected string Tenant = "LosAlamos";
        protected string Environment = "unitest";

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
                    Region = "DefaultRegion",
                    Component = Component,
                    Tenant = Tenant,
                    Environment = Environment
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

        // ToDo: Surely this can be made better?
        private void ConfigureNotificationStackMock(FluentNotificationStack fns)
        {
            NotificationStack = Substitute.For<INotificationStack>();

            var notificationStackField = fns.GetType().GetField("Stack", BindingFlags.Instance | BindingFlags.NonPublic);

            var constructedStack = (NotificationStack)notificationStackField.GetValue(fns);

            NotificationStack.Config.Returns(constructedStack.Config);

            notificationStackField.SetValue(fns, NotificationStack);
        }

        private void ConfigureAmazonQueueCreator(FluentNotificationStack fns)
        {
            fns.GetType().BaseType
                .GetField("_amazonQueueCreator", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(fns, Substitute.For<IVerifyAmazonQueues>());
        }

        protected override void When()
        {
            throw new NotImplementedException();
        }
    }
}