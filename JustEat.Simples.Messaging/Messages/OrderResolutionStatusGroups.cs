namespace JustEat.Simples.NotificationStack.Messaging.Messages
{
    public class OrderResolutionStatusGroups
    {
        public static OrderResolutionStatus[] AcceptedStates = new[]
        {
            OrderResolutionStatus.Ok,
            OrderResolutionStatus.OkByBox,
            OrderResolutionStatus.OkByPhone,
            OrderResolutionStatus.OkPhonedRestaurant,
            OrderResolutionStatus.OkResentOnce,
            OrderResolutionStatus.OkResentTwice,

        };

        public static OrderResolutionStatus[] CancelledStates = new[]
        {
            OrderResolutionStatus.DeletedTooFar,
            OrderResolutionStatus.DeletedFakeDelivered,
            OrderResolutionStatus.DeletedFakeNotDelivered,
            OrderResolutionStatus.DeletedFaxNotReceived,
            OrderResolutionStatus.DeletedIncorrectOrder,
            OrderResolutionStatus.DeletedNoAnswerFromRestaurant,
            OrderResolutionStatus.DeletedMissedByRestaurant,
            OrderResolutionStatus.DeletedFakeNotCollected,
            OrderResolutionStatus.DeletedRestaurantCannotDeliver,
            OrderResolutionStatus.DeletedRejectedByRestaurant,
            OrderResolutionStatus.DeletedNewTimeRejected,
            OrderResolutionStatus.DeletedSystemError,
            OrderResolutionStatus.DeletedTestOrder
        };
    }
}