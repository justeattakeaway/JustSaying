using CanaryDemo.Shared;
using JustSaying;
using JustSaying.Messaging;
using JustSaying.Messaging.MessageHandling;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleApp;

// One consumer "pod": a plain JustSaying subscriber plus the PWM gate throttle
// (GatedReceiveMiddleware). Everything it knows arrives through configuration —
// pods never talk to each other.

var builder = Host.CreateApplicationBuilder(args);

string Required(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing required configuration '{key}'.");

string podName = Required("POD_NAME");
string poolName = Required("POOL_NAME");
string queueName = Required("QUEUE_NAME");
string region = builder.Configuration["AWS_REGION"] ?? "eu-west-1";
var receiveWait = TimeSpan.FromSeconds(double.Parse(builder.Configuration["RECEIVE_WAIT_SECONDS"] ?? "1"));
var handlerWork = TimeSpan.FromMilliseconds(double.Parse(builder.Configuration["HANDLER_WORK_MS"] ?? "5"));

// The in-app canary gate is opt-in: it exists only if a weights file is configured.
// With no WEIGHTS_FILE this is a completely vanilla JustSaying consumer — which is the
// whole pod when the traffic shaping happens in a proxy between it and SQS instead.
string weightsFile = builder.Configuration["WEIGHTS_FILE"];
var pwmPeriod = TimeSpan.FromSeconds(double.Parse(builder.Configuration["PWM_PERIOD_SECONDS"] ?? "10"));

// When SQS_ENDPOINT is set we point the SDK at floci; when it isn't, the default
// AWS credential chain and real SQS are used, like any production service.
string sqsEndpoint = builder.Configuration["SQS_ENDPOINT"];

// stdout carries the STAT lines the orchestrator reads; send logs to stderr.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(Enum.TryParse<LogLevel>(builder.Configuration["LOG_LEVEL"], out var level) ? level : LogLevel.Warning);

builder.Services
    .AddSingleton<HandledCounter>()
    .AddSingleton<IHandlerAsync<CanaryOrder>>(sp => new OrderHandler(sp.GetRequiredService<HandledCounter>(), handlerWork))
    .AddJustSaying((config, sp) =>
    {
        config.Messaging(m => m.WithRegion(region));
        if (sqsEndpoint is not null)
        {
            string accountId = Required("AWS_ACCOUNT_ID");
            config.Client(c => c.WithClientFactory(() => new FlociClientFactory(new Uri(sqsEndpoint), accountId, region)));
        }

        config.Subscriptions(sub =>
        {
            // Small prefetch/multiplexer matter for PWM fidelity: throttling only stops
            // fetching, so anything already buffered in-process is still handled during
            // the off-window. JustSaying's defaults (prefetch 10, multiplexer 100) let a
            // pod hoard >100 messages per on-window under backlog, which flattens the
            // duty cycle; keeping the in-process pipeline shallow bounds that carryover.
            // The canary throttle: the PWM gate holds each poll until the on-window.
            // Note the receive wait (1s) is deliberately well under the PWM period —
            // the last poll of a window lingers up to one wait into the off-window.
            sub.WithDefaults(d =>
            {
                d.WithDefaultReceiveMessagesWaitTime(receiveWait)
                    .WithDefaultConcurrencyLimit(8)
                    .WithDefaultPrefetch(5)
                    .WithDefaultMultiplexerCapacity(10);

                if (weightsFile is not null)
                {
                    var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
                    d.WithCustomMiddleware(new GatedReceiveMiddleware(
                        new PwmGate(
                            new PoolWeightWatcher(weightsFile, poolName, loggerFactory.CreateLogger<PoolWeightWatcher>()),
                            pwmPeriod),
                        loggerFactory.CreateLogger<JustSaying.Messaging.Middleware.Receive.DefaultReceiveMessagesMiddleware>()));
                }
            });
            sub.ForQueue<CanaryOrder>(q => q.WithQueueName(queueName));
        });
    });

builder.Services
    .AddHostedService<BusRunnerService>()
    .AddHostedService(sp => new StatsReporter(sp.GetRequiredService<HandledCounter>(), poolName, podName));

await builder.Build().RunAsync();
