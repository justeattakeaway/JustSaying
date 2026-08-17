# Canary polling demo

**Question:** can two pools of JustSaying pods consuming the *same* SQS queue split traffic
roughly N/100−N purely by adjusting their own polling, given only a broadcast signal
(no pod-to-pod communication, no infrastructure routing, no pod scaling)?

**Answer: yes — validated against real AWS SQS across all three traffic regimes.** The
mechanism is pulse-width modulation (PWM) of JustSaying's built-in
`IMessageReceivePauseSignal`, using public API only — no changes to JustSaying itself.

## Layout

```
SampleApp/   the consumer "pod" — what a real service would ship
  Program.cs             plain JustSaying subscriber wired up with the pieces below
  GatedReceiveMiddleware.cs ← the mechanism: a PWM gate in the receive middleware —
                         cooperative, never cancels a poll (PwmGate is the clock)
  PoolWeightWatcher.cs   the rollout signal: a watched weights file (ConfigMap-shaped)
  OrderHandler.cs        stand-in message processing + demo stats line
  BusRunnerService.cs    starts the bus
Demo/        demo/load orchestration only — nothing here is part of the mechanism
  Program.cs             starts floci or targets real AWS, spawns pods as separate OS
                         processes, writes the weights file, generates load, reports
  PodProcess.cs          process launch + stdout stats collection
Shared/      message contract + floci-pointed AWS client factory
```

Pods run as separate OS processes because that's the honest version of "pods cannot
communicate": the only things they share are the queue and the signal file.

## The mechanism

`GatedReceiveMiddleware` sits in JustSaying's receive pipeline
(`Subscriptions.WithDefaults(d => d.WithCustomMiddleware(...))`) and enforces a PWM
clock: for `weight × period` of each cycle polls pass through untouched — the pod
competes for messages *exactly like an unthrottled pod* — and for the rest of the cycle
the next poll simply isn't started, at a random phase per pod. Because an on-window pod
is indistinguishable from an unthrottled one, the achieved share depends only on the
duty cycle — not on how the broker arbitrates between concurrent long-pollers. And
because a poll, once issued, always completes naturally, no message is ever stranded
mid-delivery: the casualty rate is zero by construction.

The one coupling this keeps: the last poll of a window lingers up to the receive wait
into the off-window, so the wait must stay well under the period (1s wait / 10s period
validated; under flowing traffic polls return in milliseconds, so the linger only exists
on an idle queue). An earlier iteration instead pulsed `IMessageReceivePauseSignal`,
which JustSaying 8.1.1 honours by *cancelling* the in-flight poll — prompt, and the
right behaviour for operational "stop consuming now", but the cancellations strand a
small percentage of messages until the visibility timeout (see the casualties section);
that variant lost the A/B and its code was removed.

Weight semantics: `1.0` = normal pod, `0.0` = fully parked (a clean "drain this pool"
switch), in between = duty cycle. The percentage → weight mapping belongs to rollout
tooling, which knows the replica counts (the demo uses weight 0.327 to target 20% with
2v2 pods; the modeled share differs a little per regime and is printed per scenario).

The signal is a JSON file mapping pool → weight, re-read on timestamp change:
`{"primary": 1.0, "canary": 0.33}`. A ConfigMap-mounted file (updated in place by
Kubernetes, no restarts) fits this exactly; an env-refreshed flag service works the same.

## Results on real AWS SQS (`--regimes --aws`, 20% canary target, 2v2 pods)

| Regime | What it exercises | Observed | Modeled |
|---|---|---|---|
| Backlog (12k pre-loaded, pods flat out) | backpressure / poll-rate share | **27.5%** | 24.6% |
| Steady (30 msg/s, queue near-empty) | continuous arrival-limited flow | **23.1%** | 20.0% |
| Idle (1 msg / 2s, all pods parked) | SQS fairness among parked long-polls | **20.0%** | ~25% |

The idle result is the important one: **real SQS distributes sparse messages roughly
uniformly among parked long-pollers**, so the split holds even when every worker is
sitting idle in an empty long poll. The weight-sweep demo (canary 0.33 → 1.0 → 0.0 under
steady load) tracks 20% → 50% → 0% within a couple of points, with changes applying in
seconds, in-place.

## Hard-won tuning rules (violate these and the split degrades badly)

1. **Keep the in-process pipeline shallow.** Pausing only stops *fetching* — anything
   already prefetched/buffered still gets processed during the off-window. With
   JustSaying's defaults (prefetch 10, multiplexer capacity 100) a canary pod hoarded
   100+ messages per on-window under backlog and the observed share was 42.5% instead of
   ~25%. With prefetch 5 / multiplexer 10 it landed at 27.5%. Bound the buffered work to
   well under one PWM off-window of processing.
2. **(Pre-8.1.1 only) Keep the receive wait well under the PWM period.** Before
   JustSaying 8.1.1, a pause didn't cancel an already-parked long poll, which lingered up
   to the wait time into the off-window — with a production-default 20s wait and a short
   period the canary never stopped listening and the split collapsed toward 50/50.
   **JustSaying 8.1.1 / 7.4.1 fix this** ([#2287](https://github.com/justeattakeaway/JustSaying/issues/2287)):
   `Pause()` now cancels the in-flight receive, and the 20s-wait + 10s-period combination
   measured 23.5% steady / ~11-29% idle (small samples) on real SQS. See the casualties
   section below for the cost.
3. **The averaging window must span many PWM periods.** A backlog that drains in 1–2
   periods gets a lumpy split (whichever pods happened to be on). Size the period so
   drains/evaluation windows cover ≥10 periods.

## Casualties under pause-cancellation (the removed variant, measured on real AWS)

Cancelling a receive that SQS is mid-way through serving leaves those messages invisible
until the visibility timeout (30s), when they are redelivered — "casualties": received but
not processed until ~30s later. Measured on real SQS with 20s waits (casualty = handled
with >15s end-to-end latency; normal latency is well under 1s):

| Config | Cancellations | Casualty rate | Notes |
|---|---|---|---|
| Period 10s (sane) | ~6/min per canary pod | **1.3–1.8%** | max latency 60–120s, nothing dead-lettered |
| Period 1s (extreme churn) | ~1/s per canary pod | **2.8–4.4%** | ~0.4% still bouncing after 2 min; **2 of 1,785 messages dead-lettered in one run (~0.1%)** |

Details worth knowing:

- Casualties bounce in ~30s steps and can bounce repeatedly (max observed 120s = 4
  bounces). With a 1s period the 30s visibility timeout is an exact multiple of the PWM
  period, so a redelivery lands at the *same phase* and can be cancelled again —
  **jitter the period** (e.g. 9–11s randomised per cycle) to break the resonance.
- Each bounce increments `ApproximateReceiveCount`, so enough bounces exhaust the error
  queue redrive and the message is **dead-lettered without ever failing in a handler**.
  Only observed in the extreme 1s-period config. Mitigations: longer/jittered period,
  and/or raise the retry count on queues subject to canary throttling.
- Messages are never lost — delayed or dead-lettered only.
- One full-length run showed a steady-phase anomaly (47% share, one canary apparently
  unthrottled for a minute) that did not reproduce across two further runs; the weight
  watcher now logs read failures so a stuck weight would be visible.

The spike now consumes the released `JustSaying` 8.1.1 packages from NuGet (see
`Directory.Packages.props`), so these numbers reflect what ships.

## The A/B that decided it (gate vs pause-signal variant)

Same PWM clock, different enforcement point: instead of pulsing the pause signal (which
cancels the in-flight poll), `GatedReceiveMiddleware` sits in the receive pipeline
(`WithCustomMiddleware`) and simply doesn't *start* the next poll until the on-window
opens. A poll, once issued, always completes naturally — so casualties are impossible by
construction. The trade: the last poll of a window lingers up to the receive wait into
the off-window, so the wait must stay well under the period (1s wait / 10s period; under
flowing traffic polls return in milliseconds anyway, so the linger only exists on an
idle queue). Works on any JustSaying version — no 8.1.1 dependency.

A/B on real AWS SQS, identical scenarios (20% target):

| | Pause signal + 8.1.1 cancellation (20s wait) | Middleware gate (1s wait) |
|---|---|---|
| Steady split | 23.5% | **19.1%** |
| Idle split | ~11–29% (small n) | **20.0%** |
| Churn split (1s period) | 16.9% | **21.4%** |
| Casualties (churn) | 2.8–4.4%, max 120s, ~0.1% DLQ'd | **0, max latency 0.2s, 0 DLQ'd** |
| Messages accounted | ~99.6% within 2 min | **100%** |

Verdict: use the gate for the always-on traffic-shaping loop; keep the pause signal
(with its 8.1.1 prompt-cancel behaviour) for what it's really for — operational "stop
consuming now". The 1s receive wait costs ~3¢/pod/day in empty requests and does not
affect delivery latency.

## Emulator caveat

Both LocalSqsSnsMessaging in-memory and floci showed **deterministic, unfair arbitration
among parked long-pollers when the queue is idle** (in the sparse test on floci, one
primary pod received *every* message; the canary got zero). All waiters park on one
`Channel<Message>` and the wake race systematically favours particular pollers. Fine for
functional tests, misleading for fairness experiments — validate distribution behaviour
on real SQS. (Possibly worth randomizing in LocalSqsSnsMessaging to better model SQS.)

## Running it

```
cd spikes/CanaryPolling/Demo
dotnet run                        # weight sweep on floci (starts a container on :4599 if needed)
dotnet run -- --regimes           # backlog/steady/idle validation on floci
dotnet run -- --regimes --aws     # the real thing: AWS SQS via your CLI credentials (~10 min;
                                  #   queues are created with a unique name and deleted after)
dotnet run -- --longpoll --aws    # steady/idle at the recommended config + 1s-period churn,
                                  #   with end-to-end latency + casualty accounting
dotnet run -- --quick             # shorter phases, noisier numbers (combines with the above)
FLOCI_URL=http://... dotnet run   # use an existing floci/SQS-compatible endpoint
```

AWS mode resolves the region from the CLI and borrows credentials via
`aws configure export-credentials`, so `aws login` / SSO sessions work.

## Notes for a real rollout

- Canary traffic arrives in bursts of `period × weight` — fine for "N% of volume over an
  evaluation window", not per-second smoothness.
- Message latency is unaffected while the primary pool runs unthrottled (it always has
  pollers live); don't invert that (primary 0, canary <1) without thinking about latency.
- The gate only defers polls; in-flight messages complete normally. No interaction with
  visibility timeouts, error queues, or redrive. (A reject/re-release scheme via
  `ChangeMessageVisibility(0)` was considered and dropped: it inflates
  `ApproximateReceiveCount` and DLQs a slice of traffic under redrive policies.)
- An earlier iteration tried pacing individual `ReceiveMessage` calls proportionally in
  the same middleware seam: exact under backlog but broker-arbitration-dependent when the
  queue is near-empty (starved to ~7% on the emulators). PWM won on robustness; the gate
  keeps PWM but enforces it at that seam.
- Productizing inside JustSaying would be a small opt-in helper wiring a weight source to
  the gate middleware (`AddCanaryPolling(...)`); everything here works today from app
  code, on any JustSaying version.
