using System.Text.Json.Serialization;
using JustSaying.Sample.Restaurant.Models;
using JustSaying.Sample.Restaurant.OrderingApi.Models;

namespace JustSaying.Sample.Restaurant.OrderingApi;

// JustSaying writes enums as strings; its default converter for that needs dynamic code, so a
// source-generated context has to opt in to keep the same wire format under Native AOT.
[JsonSourceGenerationOptions(UseStringEnumConverter = true)]
[JsonSerializable(typeof(CustomerOrderModel))]
[JsonSerializable(typeof(IReadOnlyCollection<CustomerOrderModel>))]
[JsonSerializable(typeof(OrderPlacedEvent))]
[JsonSerializable(typeof(OrderReadyEvent))]
[JsonSerializable(typeof(OrderDeliveredEvent))]
[JsonSerializable(typeof(OrderOnItsWayEvent))]
public sealed partial class ApplicationJsonContext : JsonSerializerContext;
