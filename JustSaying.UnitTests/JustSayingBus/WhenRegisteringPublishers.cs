using System.Linq;
using System.Threading.Tasks;
using JustBehave;
using JustSaying.Messaging;
using JustSaying.TestingFramework;
using NSubstitute;
using Shouldly;

namespace JustSaying.UnitTests.JustSayingBus
{
    public class WhenRegisteringPublishers : GivenAServiceBus
    {
        private IMessagePublisher _publisher;

        protected override void Given()
        {
            base.Given();
            _publisher = Substitute.For<IMessagePublisher>();
        }

        protected override async Task When()
        {
            SystemUnderTest.AddMessagePublisher<OrderAccepted>(_publisher, string.Empty);
            SystemUnderTest.AddMessagePublisher<OrderRejected>(_publisher, string.Empty);

            await SystemUnderTest.PublishAsync(new OrderAccepted());
            await SystemUnderTest.PublishAsync(new OrderRejected());
            await SystemUnderTest.PublishAsync(new OrderRejected());
        }

        [Then]
        public void AcceptedOrderWasPublishedOnce()
        {
            _publisher.Received(1).PublishAsync(Arg.Any<OrderAccepted>());
        }

        [Then]
        public void RejectedOrderWasPublishedTwice()
        {
            _publisher.Received(2).PublishAsync(Arg.Any<OrderRejected>());
        }

        [Then]
        public void AndInterrogationShowsPublishersHaveBeenSet()
        {
            var response = SystemUnderTest.WhatDoIHave();

            response.Publishers.Count().ShouldBe(2);
            response.Publishers.First(x => x.MessageType == typeof (OrderAccepted)).ShouldNotBe(null);
            response.Publishers.First(x => x.MessageType == typeof(OrderRejected)).ShouldNotBe(null);
        }
    }
}
