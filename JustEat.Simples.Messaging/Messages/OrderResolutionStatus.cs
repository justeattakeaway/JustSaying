namespace JustEat.Simples.NotificationStack.Messaging.Messages
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
        OkByBox = 15,
        DeletedRejectedByRestaurant = 16,
        DeletedNewTimeRejected = 17,
        DeletedSystemError = 18,
        DeletedTestOrder = 25,
        OkByPhone = 8,
        OkResentOnce = 5,
        OkResentTwice = 6,
    }
}