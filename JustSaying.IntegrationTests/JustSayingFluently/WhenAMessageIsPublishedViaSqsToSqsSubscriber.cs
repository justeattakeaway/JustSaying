using JustSaying.Messaging.MessageHandling;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.IntegrationTests.JustSayingFluently
{
    public class WhenAMessageIsPublishedViaSqsToSqsSubscriber : GivenANotificationStack
    {
        private readonly Future<SqsPublishedMessage> _sqsHandler = new Future<SqsPublishedMessage>();

        protected override void Given()
        {
            base.Given();
            var handler = Substitute.For<IHandler<SqsPublishedMessage>>();
            handler.When(x => x.Handle(Arg.Is<SqsPublishedMessage>(y => y.HelloText == "Hiya...")))
                .Do(x => _sqsHandler.Complete((SqsPublishedMessage)x.Args()[0]));
        }

        protected override IAmJustSayingFluently CreateSystemUnderTest()
        {
            var sut =  base.CreateSystemUnderTest();
            
            ServiceBus
                .WithSqsMessagePublisher<SqsPublishedMessage>(x => { });

            return sut;
        }

        protected override void When()
        {
            ServiceBus.Publish(new SqsPublishedMessage{HelloText = "Hiya..."});
        }

        [Test]
        public void ThenItGetsHandled()
        {
            _sqsHandler.WaitUntilCompletion(2.Seconds()).ShouldBeTrue();
        }

        public class SqsPublishedMessage : Models.Message
        {
            public string HelloText { get; set; }
        }
    }
}