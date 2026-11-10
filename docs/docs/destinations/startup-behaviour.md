---
---

# Infrastructure on Startup

When the publisher or the bus starts, JustSaying makes sure every [owned destination](/destinations/#owned-and-existing-resources) exists. An existing destination (one addressed by ARN or URL) is never created or changed: use `WithQueueExistenceCheck()` if you want startup to fail when an existing queue is missing.

## Creating a queue

A `QueueDestination` gives the same queue whichever registration creates it. Unless it opts out with `WithNoErrorQueue()`, an owned queue gets:

* the queue itself, with the settings its destination declares and the defaults for the rest;
* an `_error` queue, with the same encryption and tags;
* a redrive policy that moves a message to the `_error` queue after `WithRetriesBeforeErrorQueue` receives (5 by default).

A topic subscription also subscribes the queue to the topic, and sets the queue's access policy to allow the topic to send to it.

## Updating a queue that already exists

On every start, an owned queue that already exists is brought in line with its destination, but how far depends on the registration:

* **A subscription** sets every setting its destination declares, and resets every setting it *doesn't* declare to the default. A subscription owns its queue, so its configuration is the whole truth about it.
* **A publication** only sets the settings its destination declares, and leaves the rest alone. A point-to-point queue usually belongs to a subscriber in another service, so `WithQueue<T>()` never resets that subscriber's visibility timeout or adds an error queue it opted out of.

:::warning
Because a subscription resets undeclared settings to their defaults, removing a setting from a subscription's destination changes the live queue on the next deploy. When porting v8 configuration, carry every `WithReadConfiguration` setting over to the `QueueDestination`: deleting the call instead resets the visibility timeout to 30 seconds, retention to 4 days, delivery delay to 0 and retries to 5, and gives a queue that had opted out of an error queue a new `_error` queue.
:::

## What is never undone

Some settings are only ever added or changed, never removed, by either kind of registration:

* **Encryption is never removed.** A destination without `WithEncryption` leaves an existing queue's or topic's KMS settings as they are; one with `WithEncryption` adds or changes them. To decrypt a queue on purpose, change it outside JustSaying.
* **Tags are only added.** A tag removed from the destination stays on the resource.
* **`WithNoErrorQueue()` doesn't delete an error queue.** It stops JustSaying creating one, but an existing redrive policy and `_error` queue are left in place.

## What is always overwritten

When a topic subscription subscribes its queue to the topic, it rewrites the queue's access `Policy` with a statement that allows the topic to send to it. Statements added to the policy outside JustSaying are replaced.
