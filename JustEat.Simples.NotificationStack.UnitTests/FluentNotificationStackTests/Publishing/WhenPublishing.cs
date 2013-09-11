using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Simples.NotificationStack.Messaging.Messages;
using JustEat.Simples.NotificationStack.Messaging.Messages.OrderDispatch;
using JustEat.Simples.NotificationStack.Stack;
using JustEat.Testing;
using NSubstitute;

namespace Stack.UnitTests.FluentNotificationStackTests.Publishing
{
    public class WhenPublishing : BehaviourTest<FluentNotificationStack>
    {
        private const string RegisterningComponent = "OrderEngine";
        private readonly Message _message = new OrderAccepted(0, 0, 0);
        private readonly INotificationStack _notificationStack = Substitute.For<INotificationStack>();

        protected override FluentNotificationStack CreateSystemUnderTest()
        {
            return new FluentNotificationStack(_notificationStack, null);
        }

        protected override void Given()
        {
            var config = Substitute.For<IMessagingConfig>();
            config.Component.Returns(RegisterningComponent);
            _notificationStack.Config.Returns(config);
        }

        protected override void When()
        {
            SystemUnderTest.Publish(_message);
        }

        [Then]
        public void TheMessageIsPublished()
        {
            _notificationStack.Received().Publish(_message);
        }

        [Then]
        public void TheMessageIsPopulatedWithComponent()
        {
            _notificationStack.Received().Publish(Arg.Is<Message>(x => x.RaisingComponent == RegisterningComponent));
        }
    }
}