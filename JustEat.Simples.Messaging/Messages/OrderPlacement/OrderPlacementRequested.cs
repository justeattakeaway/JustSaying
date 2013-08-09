namespace JustEat.Simples.NotificationStack.Messaging.Messages.OrderPlacement
{
    public class OrderPlacementRequested : Message
    {
        public OrderPlacementRequested(int basketId, string orderNotes)
        {
            BasketId = basketId;
            OrderNotes = orderNotes;
        }

        public int BasketId { get; private set; }
        public string OrderNotes { get; private set; }
    }
}