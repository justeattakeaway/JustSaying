namespace JustSaying.Fluent;

public sealed class QueueAddressConfiguration
{
    public string SubscriptionGroupName { get; set; }
    public bool RawMessageDelivery { get; set; }

    public void Validate()
    {
        // TODO
    }
}
