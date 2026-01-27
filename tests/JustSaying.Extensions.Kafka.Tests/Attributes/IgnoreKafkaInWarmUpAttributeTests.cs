using JustSaying.Extensions.Kafka.Attributes;
using JustSaying.Extensions.Kafka.Messaging;

namespace JustSaying.Extensions.Kafka.Tests.Attributes;

public class IgnoreKafkaInWarmUpAttributeTests
{
    [Fact]
    public void KafkaMessageConsumer_HasIgnoreWarmUpAttribute()
    {
        // Assert
        var attribute = Attribute.GetCustomAttribute(
            typeof(KafkaMessageConsumer),
            typeof(IgnoreKafkaInWarmUpAttribute));

        Assert.NotNull(attribute);
    }

    [Fact]
    public void KafkaProducer_HasIgnoreWarmUpAttribute()
    {
        // Assert - check the generic type definition
        var producerType = typeof(KafkaProducer<>);
        var attribute = Attribute.GetCustomAttribute(
            producerType,
            typeof(IgnoreKafkaInWarmUpAttribute));

        Assert.NotNull(attribute);
    }

    [Fact]
    public void KafkaConsumerWorker_HasIgnoreWarmUpAttribute()
    {
        // Assert - check the generic type definition
        var workerType = typeof(KafkaConsumerWorker<>);
        var attribute = Attribute.GetCustomAttribute(
            workerType,
            typeof(IgnoreKafkaInWarmUpAttribute));

        Assert.NotNull(attribute);
    }

    [Fact]
    public void IgnoreKafkaInWarmUpAttribute_CanBeAppliedToClass()
    {
        // Arrange
        var attribute = new IgnoreKafkaInWarmUpAttribute();

        // Assert
        var usage = Attribute.GetCustomAttribute(
            typeof(IgnoreKafkaInWarmUpAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        Assert.NotNull(usage);
        Assert.True((usage.ValidOn & AttributeTargets.Class) == AttributeTargets.Class);
    }

    [Fact]
    public void IgnoreKafkaInWarmUpAttribute_CanBeAppliedToInterface()
    {
        // Assert
        var usage = Attribute.GetCustomAttribute(
            typeof(IgnoreKafkaInWarmUpAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        Assert.NotNull(usage);
        Assert.True((usage.ValidOn & AttributeTargets.Interface) == AttributeTargets.Interface);
    }

    [Fact]
    public void IgnoreKafkaInWarmUpAttribute_IsInherited()
    {
        // Assert
        var usage = Attribute.GetCustomAttribute(
            typeof(IgnoreKafkaInWarmUpAttribute),
            typeof(AttributeUsageAttribute)) as AttributeUsageAttribute;

        Assert.NotNull(usage);
        Assert.True(usage.Inherited);
    }

    [Fact]
    public void CanFilterServicesWithAttribute()
    {
        // Arrange
        var types = new[]
        {
            typeof(KafkaMessageConsumer),
            typeof(KafkaProducer<>),
            typeof(string),
            typeof(int)
        };

        // Act
        var filteredTypes = types.Where(t =>
            !t.IsDefined(typeof(IgnoreKafkaInWarmUpAttribute), false)).ToArray();

        // Assert
        Assert.Equal(2, filteredTypes.Length);
        Assert.Contains(typeof(string), filteredTypes);
        Assert.Contains(typeof(int), filteredTypes);
    }
}

