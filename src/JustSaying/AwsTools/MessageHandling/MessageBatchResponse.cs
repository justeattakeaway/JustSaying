using System.Net;
using Amazon.Runtime;
using JustSaying.Messaging;

namespace JustSaying.AwsTools.MessageHandling;

public class MessageBatchResponse
{
    public IEnumerable<string> SuccessfulMessageIds { get; set; }
    public IEnumerable<string> FailedMessageIds { get; set; }
    public ResponseMetadata ResponseMetadata { get; set; }
    public HttpStatusCode? HttpStatusCode { set; get; }
}
