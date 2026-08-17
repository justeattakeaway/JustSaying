# Issue draft for justeattakeaway/LocalSqsSnsMessaging

**Title:** Server mode: long-poll delivery is winner-takes-all when messages arrive slower than the wait time (real SQS is roughly uniform)

---

While spiking a canary rollout mechanism for JustSaying (splitting traffic between two pools of consumers on one queue by throttling their polling), I got results against floci that didn't reproduce on real SQS. The functional behaviour is spot on, but the _distribution_ behaviour — which waiting receiver gets the next message — diverges in a way that can mislead any test involving competing consumers.

## Repro

4 identical consumers long polling one standard queue with `WaitTimeSeconds = 1`, and a producer sending one message every 2 seconds — so every consumer's poll expires empty and re-parks between arrivals:

```csharp
var sqs = new AmazonSQSClient(
    new BasicAWSCredentials("123456789012", "secret"),
    new AmazonSQSConfig { ServiceURL = "http://localhost:4566", AuthenticationRegion = "eu-west-1" });

var queueUrl = (await sqs.CreateQueueAsync("fairness")).QueueUrl;

var counts = new int[4];
for (int i = 0; i < 4; i++)
{
    int consumer = i;
    _ = Task.Run(async () =>
    {
        while (true)
        {
            var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = queueUrl,
                WaitTimeSeconds = 1,
            });
            foreach (var message in response.Messages ?? [])
            {
                Interlocked.Increment(ref counts[consumer]);
                await sqs.DeleteMessageAsync(queueUrl, message.ReceiptHandle);
            }
        }
    });
}

await Task.Delay(500);
for (int i = 0; i < 30; i++)
{
    await sqs.SendMessageAsync(queueUrl, $"message {i}");
    await Task.Delay(2000);
}
```

Against `floci/floci:latest`, one consumer receives (almost) every message:

```text
consumer counts: 0, 30, 0, 0
consumer counts: 5, 0, 0, 25   (second run — the winner can shift, but it's winner-takes-all)
```

Against real SQS (eu-west-1, same code), the spread is roughly even, consistent with SQS picking ~uniformly among whoever is parked when a message lands. The in-memory `InMemoryAwsBus` is also fair here (`6, 9, 7, 8`), so this looks specific to the server path.

The trigger seems to be the arrival gap exceeding the wait time. Bump the wait to `5` (so polls span the gaps) and the server is fair again: `7, 6, 11, 6`. It's exactly the idle-queue case — sparse traffic, all workers parked — where it bites.

## What I think is going on

In `InternalSqsClient.ReceiveMessageAsync` (standard queue path), every waiting receive awaits `WaitToReadAsync` on the queue's shared `Messages` channel, and the woken waiters race `TryRead`. Two things fall out of that:

- The race winner is decided by continuation scheduling, not anything random. In-process there's enough jitter (and the winner re-parks last, which rotates the order), but over HTTP the timeout/re-park cycle appears to settle into a stable order, so the same waiter keeps winning.
- A woken waiter that loses the race returns an **empty response immediately**, even with most of its `WaitTimeSeconds` remaining — real SQS would hold the poll open until a message arrives or the wait elapses. That's arguably its own divergence, and it feeds the first one (losers re-park constantly).

## Why it matters

Anything measuring how work spreads across competing consumers — canary traffic splits, work-stealing balance, autoscaling experiments — passes functionally but gives the wrong answer. In my case a throttled consumer pool measured 0% share against floci and 20% against real SQS with identical code, which sent me down a rabbit hole before I twigged it was the emulator.

My instinct for a fix would be an explicit collection of parked receive requests, with delivery handing the message to one picked at random, and losers continuing to wait out their remaining `WaitTimeSeconds` — that would sort both divergences in one go. Is that a direction you'd be happy with? Happy to have a go at a PR.
