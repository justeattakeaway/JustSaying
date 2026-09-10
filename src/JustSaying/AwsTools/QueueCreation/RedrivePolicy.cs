using System.Text.Json;
using System.Text.Json.Serialization;

namespace JustSaying.AwsTools.QueueCreation;

internal sealed class RedrivePolicy
{
    [JsonPropertyName("maxReceiveCount")]
    public int MaximumReceives { get; set; }

    [JsonPropertyName("deadLetterTargetArn")]
    public string DeadLetterQueue { get; set; }

    public RedrivePolicy(int maximumReceives, string deadLetterQueue)
    {
        MaximumReceives = maximumReceives;
        DeadLetterQueue = deadLetterQueue;
    }

    public override string ToString()
    {
#if NET8_0_OR_GREATER
        return JsonSerializer.Serialize(this, JustSayingSerializationContext.Default.RedrivePolicy);
#else
        return JsonSerializer.Serialize(this);
#endif
    }

    public static RedrivePolicy ConvertFromString(string policy)
    {
#if NET8_0_OR_GREATER
        return JsonSerializer.Deserialize(policy, JustSayingSerializationContext.Default.RedrivePolicy);
#else
        return JsonSerializer.Deserialize<RedrivePolicy>(policy);
#endif
    }
}
