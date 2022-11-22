using System.Net;
using Amazon.Runtime;

namespace JustSaying.AwsTools.MessageHandling;

public class MessageBatchResponse
{
    public IReadOnlyCollection<string> SuccessfulMessageIds { get; set; }
    public IReadOnlyCollection<string> FailedMessageIds { get; set; }
    public ResponseMetadata ResponseMetadata { get; set; }
    public HttpStatusCode? HttpStatusCode { set; get; }
}
