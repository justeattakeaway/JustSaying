using System;
using System.Threading.Tasks;
using JustSaying.AwsTools.QueueCreation;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Messaging.MessageSerialisation;
using JustSaying.Models;
using NSubstitute;
using Shouldly;
using Xunit;

namespace JustSaying.UnitTests.JustSayingFluently.AddingHandlers
{
    public class WhenSubscribingPointToPoint : JustSayingFluentlyTestBase
    {
        private readonly IHandlerAsync<Message> _handler = Substitute.For<IHandlerAsync<Message>>();
        private object _response;

        protected override void Given()
        {
        }

        protected override Task When()
        {
            _response = SystemUnderTest
                .WithSqsPointToPointSubscriber()
                .IntoDefaultQueue()
                .ConfigureSubscriptionWith(cfg => { })
                .WithMessageHandler(_handler);

            return Task.CompletedTask;
        }

        [Fact]
        public void TheQueueIsCreatedInEachRegion()
        {
            QueueVerifier.Received().EnsureQueueExistsAsync("defaultRegion", Arg.Any<SqsReadConfiguration>());
            QueueVerifier.Received().EnsureQueueExistsAsync("failoverRegion", Arg.Any<SqsReadConfiguration>());
        }

        [Fact]
        public void TheSubscriptionIsCreatedInEachRegion()
        {
            Bus.Received(2).AddNotificationSubscriber(Arg.Any<string>(), Arg.Any<INotificationSubscriber>());
        }

        [Fact]
        public void HandlerIsAddedToBus()
        {
            Bus.Received().AddMessageHandler(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Func<IHandlerAsync<Message>>>());
        }

        [Fact]
        public void SerialisationIsRegisteredForMessage()
        {
            Bus.SerialisationRegister.Received().AddSerialiser<Message>(Arg.Any<IMessageSerialiser>());
        }

        [Fact]
        public void ICanContinueConfiguringTheBus()
        {
            _response.ShouldBeAssignableTo<IFluentSubscription>();
        }

        [Fact]
        public void NoTopicIsCreated()
        {
            QueueVerifier
                .DidNotReceiveWithAnyArgs()
                .EnsureTopicExistsWithQueueSubscribedAsync(Arg.Any<string>(), Arg.Any<IMessageSerialisationRegister>(), Arg.Any<SqsReadConfiguration>());
        }
    }
}
