using System.Threading.Tasks;

namespace JustSaying.AwsTools.MessageHandling
{
    internal interface ITopicArnProvider
    {
        Task<bool> ArnExistsAsync();
        Task<string> GetArnAsync();
    }
}
