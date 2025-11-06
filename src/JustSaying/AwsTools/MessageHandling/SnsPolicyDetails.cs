using System.Collections.Generic;

namespace JustSaying.AwsTools.MessageHandling
{
    internal class SnsPolicyDetails
    {
        public IReadOnlyCollection<string> AccountIds { get; set; }
        public string SourceArn { get; set; }
    }
}
