namespace JustSaying.Naming
{
    public interface IDefaultTopicNamingConvention
    {
        string TopicName<T>();
    }
}
