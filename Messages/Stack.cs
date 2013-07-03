using System;
using System.Collections.Generic;
using SimplesNotificationStack.Messaging.MessageHandling;
using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;

namespace SimplesNotificationStack.Messaging
{
    public class Stack
    {
        private static readonly Stack Instance = new Stack();
        private IHandlerMap _handlerMap;

        public Stack(IHandlerMap handlerMap)
        {
            _handlerMap = handlerMap;
        }

        public Stack()
        {
        }

        public static Stack Register()
        {
            // Register handlers etc...

            // Do any required subscriptions etc (may require component etc to be passed)

            Instance.RegisterSerialisationMaps();
            return Instance;
        }

        public Stack WithEventHandler(IHandler<Message> handler)
        {
            _handlerMap.RegisterHandler(handler);
            return Instance;
        }

        public Stack WithSqsTopicHandling(Component component, NotificationTopic topic)
        {

            return this;
        }

        public Stack WithDefaultHandlerMap()
        {
            if (_handlerMap == null)
            {
                _handlerMap = new HandlerMap();
                return this;
            }
            throw new InvalidOperationException("Event handler map already assigned");
        }

        private void RegisterSerialisationMaps()
        {
            if (SerialisationMap.IsRegistered)
                return;

            // ToDo: Reflect messages to add serialisers.
            SerialisationMap.Register(new NewtonsoftBaseSerialiser<CustomerOrderRejectionSms>());
            SerialisationMap.Register(new NewtonsoftBaseSerialiser<CustomerOrderRejectionSmsFailed>());
        }
    }

}
