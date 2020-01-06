namespace JustSaying.Naming
{
    public interface IDefaultQueueNamingConvention
    {
        string QueueName<T>();
    }
}
