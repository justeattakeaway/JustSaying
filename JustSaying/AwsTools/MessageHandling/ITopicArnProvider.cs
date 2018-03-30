using System.Threading.Tasks;

namespace JustSaying.AwsTools.MessageHandling
{
    public interface ITopicArnProvider
    {
        Task<bool> ArnExistsAsync();
        Task<string> GetArnAsync();
    }
}
