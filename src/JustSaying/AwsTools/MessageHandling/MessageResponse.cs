using System.Net;
using Amazon.Runtime;

namespace JustSaying.AwsTools.MessageHandling;

public class MessageResponse
{
    public string MessageId { set; get; }
    public ResponseMetadata ResponseMetadata { get; set; }
    public HttpStatusCode? HttpStatusCode { set; get; }
}