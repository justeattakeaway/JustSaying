# Canary polling demo

**Question:** can two pools of JustSaying pods consuming the *same* SQS queue split traffic
roughly N/100−N purely by adjusting their own polling, given only a broadcast signal
(no pod-to-pod communication, no infrastructure routing, no pod scaling)?

**Answer: yes — validated against real AWS SQS across backlog, steady and idle traffic,
with zero stranded messages.** The mechanism is a pulse-width-modulation (PWM) gate in
JustSaying's receive middleware — public API only, no changes to JustSaying itself, and
no dependency on any particular JustSaying version.

## Layout

```
SampleApp/   the consumer "pod" — what a real service would ship
  Program.cs             plain JustSaying subscriber wired up with the pieces below
  GatedReceiveMiddleware.cs ← the mechanism: a PWM gate in the receive middleware —
                         cooperative, never cancels a poll (PwmGate is the clock)
  PoolWeightWatcher.cs   the rollout signal: a watched weights file (ConfigMap-shaped)
  OrderHandler.cs        stand-in message processing + demo stats line
  BusRunnerService.cs    starts the bus
SqsProxy/    the infra-level alternative (the Istio model): an SQS-aware proxy that
             runs the same PWM gate between vanilla pods and the queue — the logic an
             Envoy ext_proc/WASM filter would host (see the proxy section below)
Demo/        demo/load orchestration only — nothing here is part of the mechanism
  Program.cs             starts floci or targets real AWS, spawns pods as separate OS
                         processes, writes the weights file, generates load, reports
  PodProcess.cs          process launch + stdout stats collection
Shared/      message contract, floci client factory, and the shared throttle
             primitives (PwmGate clock + PoolWeightWatcher signal)
```

Pods run as separate OS processes because that's the honest version of "pods cannot
communicate": the only things they share are the queue and the signal file.

## The mechanism

`GatedReceiveMiddleware` sits in JustSaying's receive pipeline
(`Subscriptions.WithDefaults(d => d.WithCustomMiddleware(...))`) and enforces a PWM
clock: for `weight × period` of each cycle polls pass through untouched — the pod
competes for messages *exactly like an unthrottled pod* — and for the rest of the cycle
the next poll simply isn't started, at a random phase per pod.

Two properties fall out of that:

- **The split doesn't depend on broker arbitration.** An on-window pod is
  indistinguishable from an unthrottled one, so the achieved share depends only on the
  duty cycle — not on how SQS picks between concurrent long-pollers.
- **No message is ever stranded.** A poll, once issued, always completes naturally and
  its messages are processed. There is nothing to cancel, so there's no way to leave a
  message invisible mid-delivery. Measured casualty rate is zero, even when pulsing at a
  1s period (see the history section for the variant where this wasn't true).

Weight semantics: `1.0` = normal pod, `0.0` = fully parked (a clean "drain this pool"
switch), in between = duty cycle. The percentage → weight mapping belongs to rollout
tooling, which knows the replica counts (the demo uses weight 0.327 to target 20% with
2v2 pods; the modeled share differs a little per regime and is printed per scenario).

The signal is a JSON file mapping pool → weight, re-read on timestamp change:
`{"primary": 1.0, "canary": 0.33}`. A ConfigMap-mounted file (updated in place by
Kubernetes, no restarts) fits this exactly; an env-refreshed flag service works the same.
Weight changes apply within a PWM cycle, in-place.

## Results on real AWS SQS (gate mechanism, 20% canary target, 2v2 pods)

Traffic regimes (`--regimes --aws`; 1s receive wait, 10s period — 2s for the backlog):

| Regime | What it exercises | Observed | Modeled |
|---|---|---|---|
| Backlog (12k pre-loaded, pods flat out) | backpressure / poll-rate share | **28.7%** | 24.6% |
| Steady (30 msg/s, queue near-empty) | continuous arrival-limited flow | **22.8%** | 20.0% |
| Idle (1 msg / 2s, all pods parked) | SQS fairness among parked long-polls | **28.3%** (n=120) | 25.4% |

Latency/casualty validation (`--longpoll --aws`), including a deliberately brutal churn
scenario — a **1-second** PWM period, i.e. the canaries stop and start roughly every
660ms under 30 msg/s of load:

| Scenario | Split | Casualties (≥15s latency) | Max latency | Accounted for |
|---|---|---|---|---|
| Steady | 19.1% | 0 | 0.2s | 100% |
| Idle | 20.0% | 0 | 0.2s | 100% |
| Churn, 1s period | 21.4% | **0** | **0.2s** | **3,486 / 3,486** |

Nothing was dead-lettered in any run. The weight-sweep demo (canary 0.33 → 1.0 → 0.0
under steady load) tracks 20% → 50% → 0% within a couple of points, with changes
applying in seconds.

The idle result answers the question this whole approach hinges on: **real SQS
distributes sparse messages roughly uniformly among parked long-pollers**, so the split
holds even when every worker is sitting idle in an empty long poll.

## Tuning rules (violate these and the split degrades)

1. **Keep the receive wait well under the PWM period.** The gate never interrupts a
   poll, so the last poll of each on-window lingers up to the wait time into the
   off-window. 1s wait with a 10s period is the validated combination; the linger only
   exists at all on an idle queue (under flowing traffic polls return in milliseconds).
   A 1s long-poll wait costs ~3¢/pod/day in empty requests and doesn't affect delivery
   latency.
2. **Keep the in-process pipeline shallow.** Gating only defers *fetching* — anything
   already prefetched/buffered still gets processed during the off-window. With
   JustSaying's defaults (prefetch 10, multiplexer capacity 100) a throttled pod hoarded
   100+ messages per on-window under backlog and its share inflated from ~25% to 42.5%.
   With prefetch 5 / multiplexer 10 it landed at 28.7%. Bound the buffered work to well
   under one off-window of processing.
3. **The averaging window must span many PWM periods.** A backlog that drains in 1–2
   periods gets a lumpy split (whichever pods happened to be on). Size the period so
   drains/evaluation windows cover ≥10 periods; 10s suits weights ≥ ~20%, stretch it as
   the weight shrinks so the on-window (`period × weight`) stays at a few seconds.

## The proxy variant: shaping at the infrastructure layer (the Istio model)

It's been suggested the same result could come from proxying the SQS API (e.g. Istio
intercepting egress to SQS) and doing the "routing" there. That intuition holds, with
one reframe: for a pull-based queue there's nothing to *route* — there's one queue and
the pods come to it — but the proxy can run **exactly the same PWM gate** on the
`ReceiveMessage` calls passing through it. `SqsProxy/` prototypes this: pods run as
**completely vanilla consumers** (in-app gate off), each pool points at its own proxy
listener, and the proxy parks off-window `ReceiveMessage` calls for up to their own
`WaitTimeSeconds` before answering with an empty poll — forwarding them if the window
opens mid-park. Requests are never mutated and never cancelled, so the SigV4 signature
and the zero-casualty property both survive. Sends, deletes and queue management pass
straight through.

"Vanilla" is literal: in proxy mode the pods receive **no canary configuration at all** —
no weights file, no PWM knobs, no flags (the in-app gate only exists when a weights file
is configured). The pod behaves exactly as any existing SQS consumer does today; an
off-window poll just looks like an empty long poll, which is ordinary SQS behaviour, not
something the application reacts to. The proxy owns the entire mechanism.

Measured (`--proxy`, floci, same sweep as the in-app gate): **17–20% / 49–51% / 0.0%**
against 20/50/0 targets, casualties 0, max end-to-end latency 0.1s — indistinguishable
from the in-app gate, with zero application involvement.

Mapping the prototype onto a real Istio deployment:

- **Interception**: a `ServiceEntry` for `sqs.<region>.amazonaws.com` brings SQS into
  the mesh; sidecars capture the egress. Because SQS is HTTPS-only, the filter can only
  see the API call if the app→sidecar leg is plaintext with **TLS origination** at the
  sidecar/egress gateway (`DestinationRule`) — a real config decision, not a default.
- **The filter**: parking a request needs async timers, which rules out Envoy's Lua
  filter. The real options are an **ext_proc** gRPC processor or a **WASM plugin** — or
  skip sidecar filters entirely and deploy this prototype as a small in-mesh egress
  service, which is operationally the simplest and what the prototype literally is.
- **Pool identity + weight**: per-`Deployment` filter config via `workloadSelector`
  (the prototype's two listener ports stand in for this), with the weight pushed to the
  filter/service by rollout tooling — the same single-writer signal as the weights file.
- **SigV4 is the sharp edge**: the Host header is signed, so a *transparent* intercept
  (Istio) preserves signatures against real AWS, but an *explicit* proxy (SDK
  `ServiceURL` pointed at it, like this local prototype) would have to re-sign every
  forwarded request. That's why `--proxy` is floci-only locally; in-cluster Istio
  doesn't have this problem.

Trade-offs vs the in-app gate: the proxy needs **no app or library changes at all** and
shapes *every* SQS consumer stack (raw SDK, AWS.Messaging, Brighter — not just
JustSaying), with one central knob. In exchange you put a new component on the critical
consumption path (proxy outage = consumption outage), it must understand both SQS wire
protocols (JSON and Query), and the TLS/identity plumbing above is real work. The
in-app gate is ~60 lines in services you already own; the proxy is the better shape if
canarying needs to cover consumers you *don't* own.

## How we got here (variants tried and rejected)

- **Proportional poll pacing** (same middleware seam, delay each poll by
  `duration / weight`): exact under backlog, but on a near-empty queue the share is
  decided by how the broker arbitrates between parked long-pollers — it starved to ~7%
  on the emulators. PWM replaced it because an on-window pod needs no arbitration
  fairness at all.
- **PWM by pulsing `IMessageReceivePauseSignal`**: worked, and drove a real JustSaying
  fix — before 8.1.1/7.4.1 a pause didn't affect the in-flight long poll, so
  production-default 20s waits swamped the off-windows
  ([#2287](https://github.com/justeattakeaway/JustSaying/issues/2287)). 8.1.1 made
  `Pause()` cancel the in-flight receive, which fixed the split (23.5% steady on a 20%
  target with stock 20s waits) but at a price: a cancelled receive can strand messages
  SQS was mid-way through serving, invisible until the 30s visibility timeout. Measured:
  1.3–1.8% of messages delayed ≥30s at a 10s period, 2.8–4.4% at a 1s period with
  multi-bounces to 120s (30s visibility being an exact multiple of the period means a
  redelivery lands at the same PWM phase and gets re-cancelled), and ~0.1% dead-lettered
  without ever failing in a handler. An A/B against the gate on identical scenarios
  (gate: better split, zero casualties, 100% accounted) settled it, and the pause-signal
  variant's code was removed from this spike. Pause + prompt cancellation remains the
  right tool for its actual job — operational "stop consuming now" — just not for an
  always-on shaping loop pulsing it six times a minute.

## Emulator caveat

Both LocalSqsSnsMessaging in-memory and floci showed **deterministic, unfair arbitration
among parked long-pollers when the queue is idle** (in one sparse test on floci, a single
pod received *every* message). All waiters park on one `Channel<Message>` and the wake
race systematically favours particular pollers. Fine for functional tests, misleading for
fairness experiments — validate distribution behaviour on real SQS. (The gate is largely
immune — that's rather the point — but the *measurements* of fairness-sensitive variants
were only trustworthy on real SQS. Write-up for the LocalSqsSnsMessaging project in
`localsqssnsmessaging-longpoll-fairness.md`.)

## Running it

```
cd spikes/CanaryPolling/Demo
dotnet run                        # weight sweep on floci (starts a container on :4599 if needed)
dotnet run -- --regimes           # backlog/steady/idle validation on floci
dotnet run -- --regimes --aws     # the real thing: AWS SQS via your CLI credentials (~10 min;
                                  #   queues are created with a unique name and deleted after)
dotnet run -- --longpoll --aws    # steady/idle at the recommended config + 1s-period churn,
                                  #   with end-to-end latency + casualty accounting
dotnet run -- --proxy             # the Istio model: vanilla pods, PWM in the SqsProxy
                                  #   (floci-only — see the proxy section on SigV4)
dotnet run -- --quick             # shorter phases, noisier numbers (combines with the above)
FLOCI_URL=http://... dotnet run   # use an existing floci/SQS-compatible endpoint
```

The spike consumes the released `JustSaying` 8.1.1 packages from NuGet (see
`Directory.Packages.props`), so results reflect what ships. AWS mode resolves the region
from the CLI and borrows credentials via `aws configure export-credentials`, so
`aws login` / SSO sessions work (note the exported token can expire during long runs; if
queue cleanup fails, delete queues prefixed `canary-orders-` manually).

## Notes for a real rollout

- Canary traffic arrives in bursts of `period × weight` — fine for "N% of volume over an
  evaluation window", not per-second smoothness.
- Message latency is unaffected while the primary pool runs unthrottled (it always has
  pollers live); don't invert that (primary 0, canary <1) without thinking about latency.
- The gate only defers polls; in-flight messages complete normally. No interaction with
  visibility timeouts, error queues, or redrive. (A reject/re-release scheme via
  `ChangeMessageVisibility(0)` was considered and dropped: it inflates
  `ApproximateReceiveCount` and DLQs a slice of traffic under redrive policies.)
- Productizing inside JustSaying would be a small opt-in helper wiring a weight source to
  the gate middleware (`AddCanaryPolling(...)`); everything here works today from app
  code, on any JustSaying version.
