using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.Messages;

namespace UnitTests.MessageHandling
{
    [TestFixture]
    public class HandlerMapTests
    {
        readonly HandlerMap _map = new HandlerMap();

        [Test]
        public void WhenHandlerIsRegisteredForMessageThenItIsReturned()
        {
            _map.RegisterHandler(new TestHandler());
            var message = new TestMessage();

            Assert.True(_map.GetHandler(message).Handle(message));
        }

        private class TestMessage : Message {}

        private class TestHandler : BaseHandler<TestMessage>
        {
            public override bool Handle(Message message)
            {
                return true;
            }
        }
    }
}
