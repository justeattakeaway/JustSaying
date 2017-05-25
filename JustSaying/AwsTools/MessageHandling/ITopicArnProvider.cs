using System.Threading.Tasks;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ITopicArnProvider
    {
        bool ArnExists();
        Task<string> GetArnAsync();
    }
}
