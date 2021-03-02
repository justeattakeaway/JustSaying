using System.Threading.Tasks;

namespace JustSaying.AwsTools.QueueCreation
{
    public interface IQueueCreator
    {
        Task<bool> CreateAsync(SqsBasicConfiguration queueConfig, int attempt = 0);
        Task DeleteAsync();
        Task UpdateRedrivePolicyAsync(RedrivePolicy requestedRedrivePolicy);
        Task EnsureQueueAndErrorQueueExistAndAllAttributesAreUpdatedAsync(SqsReadConfiguration queueConfig);
        Task<bool> ExistsAsync();
        Task UpdateQueueAttributeAsync(SqsBasicConfiguration queueConfig);
    }
}
