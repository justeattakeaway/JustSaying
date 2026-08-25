using System.Text;
using System.Text.Json;
using CanaryDemo.Shared;
using Microsoft.AspNetCore.Http.Extensions;

// An SQS-API-aware forward proxy implementing the canary PWM gate at the network level,
// so consumer pods need NO application-level throttle at all — they are completely
// vanilla SQS clients pointed at (or transparently routed through) this proxy.
//
// This is a stand-in for what an Istio deployment would run in the sidecar/egress path:
// the request classification below ("is this ReceiveMessage, what's its WaitTimeSeconds")
// and the park-or-forward decision are exactly the logic an EnvoyFilter would host — as
// an ext_proc gRPC processor or a WASM filter (Envoy's Lua filter can't asynchronously
// delay, which this needs). Pool identity comes from which listener port the pod hits;
// in Istio it would come from workload labels selecting per-Deployment filter config.
//
// The gate is cooperative, same as the in-app variant: a ReceiveMessage arriving in the
// off-window is *parked* for up to its own WaitTimeSeconds (preserving long-poll
// semantics — the pod just sees an empty poll), and forwarded if the window opens
// mid-park. Requests are never mutated (SigV4 signatures stay valid) and never
// cancelled (no messages stranded mid-delivery). Everything that isn't ReceiveMessage —
// sends, deletes, queue management — passes straight through.
//
// Configuration (env):
//   UPSTREAM            SQS-compatible endpoint to forward to (e.g. floci)
//   PRIMARY_PORT        listener for primary-pool pods (weight from key "primary")
//   CANARY_PORT         listener for canary-pool pods (weight from key "canary")
//   WEIGHTS_FILE        the rollout signal: {"primary":1.0,"canary":0.33}
//   PWM_PERIOD_SECONDS  PWM period (default 10)

var builder = WebApplication.CreateBuilder(args);

string Required(string key) =>
    builder.Configuration[key] ?? throw new InvalidOperationException($"Missing required configuration '{key}'.");

var upstream = new Uri(Required("UPSTREAM"));
int primaryPort = int.Parse(Required("PRIMARY_PORT"));
int canaryPort = int.Parse(Required("CANARY_PORT"));
string weightsFile = Required("WEIGHTS_FILE");
var pwmPeriod = TimeSpan.FromSeconds(double.Parse(builder.Configuration["PWM_PERIOD_SECONDS"] ?? "10"));

builder.WebHost.UseUrls($"http://127.0.0.1:{primaryPort}", $"http://127.0.0.1:{canaryPort}");
builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);
builder.Logging.SetMinimumLevel(LogLevel.Warning);

var app = builder.Build();

var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var gates = new Dictionary<int, PwmGate>
{
    [primaryPort] = new(new PoolWeightWatcher(weightsFile, "primary", loggerFactory.CreateLogger<PoolWeightWatcher>()), pwmPeriod),
    [canaryPort] = new(new PoolWeightWatcher(weightsFile, "canary", loggerFactory.CreateLogger<PoolWeightWatcher>()), pwmPeriod),
};

var http = new HttpClient(new SocketsHttpHandler { AllowAutoRedirect = false })
{
    BaseAddress = upstream,
    Timeout = TimeSpan.FromSeconds(100),
};

// Hop-by-hop headers, plus ones HttpClient computes itself.
string[] skipRequestHeaders = ["Host", "Content-Length", "Connection", "Transfer-Encoding", "Keep-Alive", "Expect"];

app.MapGet("/healthz", () => "ok");

app.Map("/{**path}", async context =>
{
    var gate = gates[context.Connection.LocalPort];

    // Buffer the body: we need it to classify the request, and again to forward it.
    byte[] body;
    using (var buffer = new MemoryStream())
    {
        await context.Request.Body.CopyToAsync(buffer, context.RequestAborted);
        body = buffer.ToArray();
    }

    // --- The canary gate: only ReceiveMessage is shaped; everything else flows.
    if (TryClassifyReceiveMessage(context.Request, body, out int waitSeconds, out bool jsonProtocol))
    {
        bool windowOpen = await gate.TryWaitForOnWindowAsync(TimeSpan.FromSeconds(waitSeconds), context.RequestAborted);
        if (!windowOpen)
        {
            // Emulate an empty long poll: the pod waited its WaitTimeSeconds and got
            // nothing, exactly as if the queue were empty. It will simply poll again.
            context.Response.StatusCode = StatusCodes.Status200OK;
            if (jsonProtocol)
            {
                context.Response.ContentType = "application/x-amz-json-1.0";
                await context.Response.WriteAsync("{}", context.RequestAborted);
            }
            else
            {
                context.Response.ContentType = "text/xml";
                await context.Response.WriteAsync(
                    """<?xml version="1.0"?><ReceiveMessageResponse xmlns="http://queue.amazonaws.com/doc/2012-11-05/"><ReceiveMessageResult/><ResponseMetadata><RequestId>canary-proxy-gated</RequestId></ResponseMetadata></ReceiveMessageResponse>""",
                    context.RequestAborted);
            }

            return;
        }
    }

    // --- Transparent forward, bytes untouched so SigV4 signatures stay intact.
    using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), context.Request.Path + context.Request.QueryString);
    if (body.Length > 0 || context.Request.ContentLength > 0)
    {
        request.Content = new ByteArrayContent(body);
    }

    foreach (var header in context.Request.Headers)
    {
        if (skipRequestHeaders.Contains(header.Key, StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        if (!request.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value))
        {
            request.Content?.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string>)header.Value);
        }
    }

    using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

    context.Response.StatusCode = (int)response.StatusCode;
    foreach (var header in response.Headers.Concat(response.Content.Headers))
    {
        if (!string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.Headers[header.Key] = header.Value.ToArray();
        }
    }

    await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
});

await app.RunAsync();

// Classifies a request as SQS ReceiveMessage and extracts its WaitTimeSeconds, for both
// wire protocols the SQS SDKs use: JSON (X-Amz-Target header, JSON body) and the older
// form-encoded Query protocol (Action=ReceiveMessage in the body).
static bool TryClassifyReceiveMessage(HttpRequest request, byte[] body, out int waitSeconds, out bool jsonProtocol)
{
    waitSeconds = 0;
    jsonProtocol = false;

    if (request.Headers.TryGetValue("X-Amz-Target", out var target) &&
        target.ToString().EndsWith(".ReceiveMessage", StringComparison.Ordinal))
    {
        jsonProtocol = true;
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("WaitTimeSeconds", out var wait) && wait.TryGetInt32(out int seconds))
            {
                waitSeconds = seconds;
            }
        }
        catch (JsonException)
        {
        }

        return true;
    }

    if (string.Equals(request.ContentType?.Split(';')[0], "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
    {
        var form = Encoding.UTF8.GetString(body);
        if (form.Split('&').Contains("Action=ReceiveMessage"))
        {
            var wait = form.Split('&').FirstOrDefault(p => p.StartsWith("WaitTimeSeconds=", StringComparison.Ordinal));
            if (wait is not null && int.TryParse(wait["WaitTimeSeconds=".Length..], out int seconds))
            {
                waitSeconds = seconds;
            }

            return true;
        }
    }

    return false;
}
