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
        => JsonSerializer.Serialize(this);

    public static RedrivePolicy ConvertFromString(string policy)
        => JsonSerializer.Deserialize<RedrivePolicy>(policy);
}
