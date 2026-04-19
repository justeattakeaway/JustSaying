---
title: OpenTelemetry
---

# OpenTelemetry

JustSaying has built-in support for [OpenTelemetry](https://opentelemetry.io/) distributed tracing and metrics. Traces are automatically created for publish and process operations, and W3C trace context is propagated through SQS message attributes so that you can correlate producers and consumers across services.

## Installation

Install the `JustSaying.Extensions.OpenTelemetry` NuGet package:

```bash
dotnet add package JustSaying.Extensions.OpenTelemetry
```

This package provides convenience extension methods for wiring up JustSaying's `ActivitySource` and `Meter` with the OpenTelemetry SDK. The core instrumentation itself lives in the main `JustSaying` library — the extension package simply makes registration easier.

## Setup

### Recommended — Single Call

The simplest way to enable both tracing and metrics is a single call on the `OpenTelemetryBuilder`:

```csharp
builder.Services.AddOpenTelemetry()
    .AddJustSayingInstrumentation()    // ← registers traces + metrics
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddOtlpExporter());
```

### Individual Registration

If you only need tracing **or** metrics, you can register them separately:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddJustSayingInstrumentation()   // traces only
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddJustSayingInstrumentation()   // metrics only
        .AddOtlpExporter());
```

### Manual Registration (Without the Extension Package)

Because JustSaying uses the standard `System.Diagnostics.ActivitySource` and `System.Diagnostics.Metrics.Meter` APIs, you don't strictly need the extension package. You can register the source names directly:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource("JustSaying"))
    .WithMetrics(metrics => metrics.AddMeter("JustSaying"));
```

The constants `JustSayingDiagnostics.ActivitySourceName` and `JustSayingDiagnostics.MeterName` (both `"JustSaying"`) are public if you prefer to reference them in code.

## Distributed Tracing

### How It Works

JustSaying automatically creates [Activities](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-instrumentation-walkthroughs) (spans) for every publish and process operation:

| Operation | Activity Name | Kind | Created In |
|-----------|--------------|------|------------|
| Publish a message | `{MessageType} publish` | `Producer` | `JustSayingBus` |
| Publish a batch | `{MessageType} publish` | `Producer` | `JustSayingBus` |
| Process a message | `{QueueName} process` | `Consumer` | `MessageDispatcher` |

### Trace Context Propagation

When a message is published, JustSaying automatically injects [W3C Trace Context](https://www.w3.org/TR/trace-context/) headers into the SQS message attributes:

- **`traceparent`** — The W3C traceparent header (e.g. `00-<trace-id>-<span-id>-01`)
- **`tracestate`** — The W3C tracestate header (if present on the current activity)

When a message is consumed, JustSaying reads these attributes and creates an [ActivityLink](https://opentelemetry.io/docs/concepts/signals/traces/#span-links) from the consumer span back to the producer span. This means you can see the full journey of a message from publisher to consumer in your tracing backend (Jaeger, Zipkin, Aspire Dashboard, etc.), even across service boundaries.

> **Note:** JustSaying uses `ActivityLink` rather than parent-child propagation. This is intentional — a consumed message's processing has its own independent lifecycle and shouldn't be a child of the publish span. Links let you navigate between the two without implying a timing dependency.

### Span Attributes

All spans include semantic attributes following the [OpenTelemetry Messaging Semantic Conventions](https://opentelemetry.io/docs/specs/semconv/messaging/):

**Producer spans** (`publish`):

| Attribute | Description | Example |
|-----------|-------------|---------|
| `messaging.operation.name` | `"publish"` | `publish` |
| `messaging.operation.type` | `"send"` | `send` |
| `messaging.message.id` | The message ID | `a1b2c3d4-...` |
| `messaging.message.type` | Full type name of the message | `MyApp.Events.OrderPlaced` |
| `messaging.batch.message_count` | Number of messages (batch only) | `5` |

**Consumer spans** (`process`):

| Attribute | Description | Example |
|-----------|-------------|---------|
| `messaging.system` | `"aws_sqs"` | `aws_sqs` |
| `messaging.destination.name` | The SQS queue name | `orderplaced-queue` |
| `messaging.operation.name` | `"process"` | `process` |
| `messaging.operation.type` | `"process"` | `process` |
| `messaging.message.id` | The SQS message ID | `a1b2c3d4-...` |
| `messaging.message.type` | Full type name of the message | `MyApp.Events.OrderPlaced` |

### Error Recording

When an exception occurs during publishing or processing, JustSaying records it on the span:

- The span status is set to `Error`
- An `exception` event is added with `exception.type`, `exception.message`, and `exception.stacktrace` attributes

This means errors show up automatically in your tracing UI.

## Metrics

JustSaying emits the following metrics through the `JustSaying` meter:

| Metric | Type | Unit | Description |
|--------|------|------|-------------|
| `messaging.client.sent.messages` | Counter | `{message}` | Number of messages a producer attempted to send. Includes an `error.type` attribute on failure. |
| `messaging.client.operation.duration` | Histogram | `s` | Duration of SQS receive operations. |
| `messaging.process.duration` | Histogram | `s` | Duration of message handler execution. Tagged with `messaging.destination.name` and `messaging.message.type`. |
| `justsaying.messages.received` | Counter | `{message}` | Number of messages received from SQS. |
| `justsaying.messages.processed` | Counter | `{message}` | Number of messages processed by handlers. Tagged with `messaging.destination.name`, `messaging.message.type`, and `error.type` on failure. |
| `justsaying.messages.throttled` | Counter | `{event}` | Number of times message receiving was throttled due to concurrency limits. |

### Example: Viewing Metrics in Grafana

Once exported (e.g. via OTLP to Prometheus), you can create dashboards for:

- **Throughput** — `rate(messaging_client_sent_messages_total[5m])` for publish rate
- **Processing latency** — `histogram_quantile(0.95, rate(messaging_process_duration_bucket[5m]))` for p95 handler duration
- **Error rate** — Filter `justsaying_messages_processed_total` by `error.type != ""` for failed messages
- **Throttling** — `rate(justsaying_messages_throttled_total[5m])` to see if consumers are being back-pressured

## Extracting the Diagnostic Constants

If you need to reference the source/meter names programmatically (for example in tests or custom instrumentation), use:

```csharp
using JustSaying.Messaging.Monitoring;

string activitySourceName = JustSayingDiagnostics.ActivitySourceName; // "JustSaying"
string meterName = JustSayingDiagnostics.MeterName;                  // "JustSaying"
```

## Full Example with .NET Aspire

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register JustSaying
builder.Services.AddJustSaying(config =>
{
    config.Messaging(x => x.WithRegion("eu-west-1"));
    config.Subscriptions(x => x.ForTopic<OrderPlaced>());
    config.Publications(x => x.WithTopic<OrderPlaced>());
});

builder.Services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();

// Register OpenTelemetry with JustSaying instrumentation
builder.Services.AddOpenTelemetry()
    .AddJustSayingInstrumentation()
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();
app.Run();
```

With this setup, publishing an `OrderPlaced` message from one service and processing it in another will produce linked traces visible in the Aspire Dashboard, Jaeger, or any OTLP-compatible backend.
