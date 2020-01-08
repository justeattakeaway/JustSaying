namespace JustSaying.Naming
{
    public interface IQueueNamingConvention
    {
        string QueueName<T>();
    }
}
