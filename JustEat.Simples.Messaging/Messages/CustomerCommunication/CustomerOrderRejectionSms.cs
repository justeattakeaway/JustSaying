using JustEat.Simples.NotificationStack.Messaging.Messages.Sms;

namespace JustEat.Simples.NotificationStack.Messaging.Messages.CustomerCommunication
{
    public class CustomerOrderRejectionSms : Message
    {
        public CustomerOrderRejectionSms(int orderId, int customerId, string telephoneNumber, SmsCommunicationActivity communicationActivity)
        {
            CommunicationActivity = communicationActivity;
            TelephoneNumber = telephoneNumber;
            CustomerId = customerId;
            OrderId = orderId;
        }

        public int OrderId { get; private set; }
        public int CustomerId { get; private set; }
        public string TelephoneNumber { get; private set; }
        public SmsCommunicationActivity CommunicationActivity { get; private set; }
    }
}