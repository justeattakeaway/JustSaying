using Amazon;

namespace JustSaying.Sample.Kafka;

public static class ConfigurationExtensions
{
    private const string KafkaBootstrapServersKey = "Kafka:BootstrapServers";
    private const string KafkaConsumerGroupKey = "Kafka:ConsumerGroup";
    private const string AWSRegionKey = "AWSRegion";

    public static string GetKafkaBootstrapServers(this IConfiguration configuration)
    {
        return configuration[KafkaBootstrapServersKey] ?? "localhost:9092";
    }

    public static string GetKafkaConsumerGroup(this IConfiguration configuration)
    {
        return configuration[KafkaConsumerGroupKey] ?? "kafka-ordering-api";
    }

    public static RegionEndpoint GetAWSRegion(this IConfiguration configuration)
    {
        var region = configuration[AWSRegionKey] ?? "us-east-1";
        return RegionEndpoint.GetBySystemName(region);
    }
}
