using System.Threading;
using JustEat.Simples.NotificationStack.Messaging;
using JustEat.Testing;
using NSubstitute;
using Tests.MessageStubs;

namespace Stack.UnitTests.NotificationStack
{
    public class WhenPublishingMessages : NotificationStackBaseTest
    {
        private readonly IMessagePublisher _publisher = Substitute.For<IMessagePublisher>();
        private const string RegisterningComponent = "OrderEngine";
        private const string Tenant = "LosAlamos";

        protected override void Given()
        {
            Config.Component.Returns(RegisterningComponent);
            Config.Tenant.Returns(Tenant);
        }

        protected override void When()
        {
            SystemUnderTest.AddMessagePublisher<GenericMessage>("OrderDispatch", _publisher);

            SystemUnderTest.Publish(new GenericMessage());
            Thread.Sleep(20);
        }

        [Then]
        public void TheMessageIsPopulatedWithComponent()
        {
            _publisher.Received().Publish(Arg.Is<GenericMessage>(x => x.RaisingComponent == RegisterningComponent));
        }

        [Then]
        public void TheMessageIsPopulatedWithTenant()
        {
            _publisher.Received().Publish(Arg.Is<GenericMessage>(x => x.Tenant == Tenant));
        }

        [Then]
        public void PublishMessageTimeStatsSent()
        {
            Monitor.Received(1).PublishMessageTime(Arg.Any<long>());
        }
    }
}