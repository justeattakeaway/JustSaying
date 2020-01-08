namespace JustSaying.Naming
{
    public interface ITopicNamingConvention
    {
        string TopicName<T>();
    }
}
