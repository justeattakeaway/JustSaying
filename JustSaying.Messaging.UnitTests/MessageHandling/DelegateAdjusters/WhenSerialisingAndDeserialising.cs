using System;
using JustEat.Testing;
using JustSaying.Messaging.MessageHandling;
using JustSaying.Models;
using JustSaying.TestingFramework;
using NSubstitute;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.MessageHandling.DelegateAdjusters
{
    public class WhenConverting : BehaviourTest<DelegateAdjuster>
    {
        readonly IHandler<OrderAccepted> _handler = Substitute.For<IHandler<OrderAccepted>>();
        private Func<Message, bool> _handleExpression;

        protected override void Given()
        {
            _handler.Handle(null).ReturnsForAnyArgs(true);
            RecordAnyExceptionsThrown();
        }

        protected override void When()
        {
            _handleExpression = DelegateAdjuster.CastArgument<Message, OrderAccepted>(x => _handler.Handle(x));
        }

        [Then]
        public void NoExceptionInCasting()
        {
            Assert.IsNull(ThrownException);
        }

        [Then]
        public void CompiledExpressionCallsHandler()
        {
            Assert.IsTrue(_handleExpression.Invoke(new OrderAccepted()));
        }
    }
}
