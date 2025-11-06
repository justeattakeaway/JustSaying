using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace JustSaying.AwsTools.QueueCreation
{
    public class RedrivePolicy
    {
        [JsonProperty("maxReceiveCount")]
        [JsonPropertyName("maxReceiveCount")]
        public int MaximumReceives { get; set; }

        [JsonProperty("deadLetterTargetArn")]
        [JsonPropertyName("deadLetterTargetArn")]
        public string DeadLetterQueue { get; set; }

        public RedrivePolicy(int maximumReceives, string deadLetterQueue)
        {
            MaximumReceives = maximumReceives;
            DeadLetterQueue = deadLetterQueue;
        }

        protected RedrivePolicy()
        {
        }

        // Cannot use System.Text.Json below as no public parameterless constructor. Change for v7?

        public override string ToString()
            => JsonConvert.SerializeObject(this);

        public static RedrivePolicy ConvertFromString(string policy)
            => JsonConvert.DeserializeObject<RedrivePolicy>(policy);
    }
}
