namespace JustSaying;

public class HandlerResolutionContext(string queueName)
{
    public string QueueName { get; private set; } = queueName;
}
