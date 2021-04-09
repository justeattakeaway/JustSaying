# Metrics

By default, JustSaying won't emit any metrics as it doesn't have any information on where or how to send them. By providing an implementation of an `IMessageMonitor`, you can emit metrics to any source you like, such as StatsD, Prometheus, or as structured logs.

JustSaying will automatically pick up the last registered implementation of `IMessageMonitor` that is registered into the DI container, so all you need to do is register one:

`services.AddSingleton<MyCustomMessageMonitor>();`

