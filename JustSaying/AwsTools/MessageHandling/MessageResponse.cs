using System.Net;

namespace JustSaying.AwsTools.MessageHandling
{
    public class MessageResponse
    {
        public string MessageId { set; get; }
        public HttpStatusCode? HttpStatusCode { set; get; }
    }
}
