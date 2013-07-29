namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderResolved
{
    public enum OrderResolutionStatus
    {
        Ok = 1,
        DeletedTooFar = 2,
        DeletedFakeDelivered = 3,
        DeletedFakeNotDelivered = 4,
        DeletedFaxNotReceived = 7,
        DeletedIncorrectOrder = 9,
        DeletedNoAnswerFromRestaurant = 10,
        DeletedMissedByRestaurant = 11,
        DeletedFakeNotCollected = 12,
        DeletedRestaurantCannotDeliver = 13,
        OkPhonedRestaurant = 14,
        DeletedRejectedByRestaurant = 16,
        DeletedNewTimeRejected = 17,
        DeletedSystemError = 18,
        DeletedTestOrder = 25
    }
}