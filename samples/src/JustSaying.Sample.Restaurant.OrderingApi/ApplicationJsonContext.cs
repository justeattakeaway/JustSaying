using System.Text.Json.Serialization;
using JustSaying.Sample.Restaurant.Models;
using JustSaying.Sample.Restaurant.OrderingApi.Models;

namespace JustSaying.Sample.Restaurant.OrderingApi;

[JsonSerializable(typeof(CustomerOrderModel))]
[JsonSerializable(typeof(OrderReadyEvent))]
[JsonSerializable(typeof(OrderDeliveredEvent))]
[JsonSerializable(typeof(OrderOnItsWayEvent))]
public sealed partial class ApplicationJsonContext : JsonSerializerContext;
