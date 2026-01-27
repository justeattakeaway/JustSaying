namespace JustSaying.Extensions.Kafka.Attributes;

/// <summary>
/// Marks a service to be excluded from startup warm-up.
/// Apply to consumers and producers that should not be instantiated during application warm-up.
/// </summary>
/// <remarks>
/// This attribute is useful when using health check warm-up patterns that iterate 
/// over all services to ensure they can be instantiated.
/// 
/// Kafka consumers and producers should not be instantiated during warm-up as they
/// establish connections to brokers.
/// 
/// Usage:
/// <code>
/// // Filter warm-up services:
/// var warmUpServices = services
///     .Where(d => !d.ServiceType.IsDefined(typeof(IgnoreKafkaInWarmUpAttribute), false))
///     .Where(d => d.ImplementationType == null || 
///                 !d.ImplementationType.IsDefined(typeof(IgnoreKafkaInWarmUpAttribute), false));
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = true)]
public class IgnoreKafkaInWarmUpAttribute : Attribute
{
}

