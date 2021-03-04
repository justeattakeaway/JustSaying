using System.Collections.Generic;
using System.Threading.Tasks;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface ITopicCreator
    {
        Task EnsurePolicyIsUpdatedAsync(IReadOnlyCollection<string> additionalSubscriberAccounts);
        Task ApplyTagsAsync();
        Task<bool> ExistsAsync();
        Task CreateAsync();
        Task CreateWithEncryptionAsync(ServerSideEncryption config);

        public string Arn { get;}
    }
}
