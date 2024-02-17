using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace JustSaying.AwsTools.QueueCreation;

public class RedrivePolicy
{
    [JsonProperty("maxReceiveCount")]
    [JsonPropertyName("maxReceiveCount")]
    public int MaximumReceives { get; set; }

    [JsonProperty("deadLetterTargetArn")]
    [JsonPropertyName("deadLetterTargetArn")]
    public string DeadLetterQueue { get; set; }

#if NET8_0_OR_GREATER
    [System.Text.Json.Serialization.JsonConstructor]
#endif
    public RedrivePolicy(int maximumReceives, string deadLetterQueue)
    {
        MaximumReceives = maximumReceives;
        DeadLetterQueue = deadLetterQueue;
    }

    protected RedrivePolicy()
    {
    }

    public override string ToString()
#if NET8_0_OR_GREATER
        => System.Text.Json.JsonSerializer.Serialize(this, JustSayingSerializationContext.Default.RedrivePolicy);
#else
        => JsonConvert.SerializeObject(this);
#endif

    public static RedrivePolicy ConvertFromString(string policy)
#if NET8_0_OR_GREATER
        => System.Text.Json.JsonSerializer.Deserialize(policy, JustSayingSerializationContext.Default.RedrivePolicy);
#else
        => JsonConvert.DeserializeObject<RedrivePolicy>(policy);
#endif
}
