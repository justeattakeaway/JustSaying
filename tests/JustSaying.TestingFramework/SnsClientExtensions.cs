using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;

namespace JustSaying.TestingFramework;

public static class SnsClientExtensions
{
    public static async Task<List<Topic>> GetAllTopics(this IAmazonSimpleNotificationService client)
    {
        var topics = new List<Topic>();
        string nextToken = null;

        do
        {
            var topicsResponse = await client.ListTopicsAsync(nextToken).ConfigureAwait(false);
            nextToken = topicsResponse.NextToken;
            topics.AddRange(topicsResponse.Topics);
        } while (nextToken != null);

        return topics;
    }
}