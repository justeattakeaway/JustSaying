namespace JustSaying.Fluent;

internal static class SqsEndpointHelper
{
    public static string GetSqsHostname(string partition, string region)
    {
        return partition switch
        {
            "aws-cn" => $"sqs.{region}.amazonaws.com.cn",
            "aws-us-gov" => $"sqs.{region}.amazonaws.com",
            "aws" => $"sqs.{region}.amazonaws.com",
            _ => throw new ArgumentException($"Unknown partition: {partition}", nameof(partition))
        };
    }
}
