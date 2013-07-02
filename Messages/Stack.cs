using SimplesNotificationStack.Messaging.MessageSerialisation;
using SimplesNotificationStack.Messaging.Messages.CustomerCommunication;

namespace SimplesNotificationStack.Messaging
{
    public static class Stack
    {
        public static void Register()
        {
            // Register handlers etc...

            // Do any required subscriptions etc (may require component etc to be passed)

            RegisterSerialisationMaps();
        }

        private static void RegisterSerialisationMaps()
        {
            if (SerialisationMap.IsRegistered)
                return;

            // ToDo: Reflect messages to add serialisers.
            SerialisationMap.Register(new NewtonsoftBaseSerialiser<CustomerOrderRejectionSms>());
            SerialisationMap.Register(new NewtonsoftBaseSerialiser<CustomerOrderRejectionSmsFailed>());
        }
    }
}
