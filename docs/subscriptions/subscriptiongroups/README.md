---
description: >-
  Subscription Groups in JustSaying are a way of managing concurrency across
  multiple queues, and applying backpressure during high-load events.
---

# SubscriptionGroups

Imagine a scenario in which you  have several queues whose handlers all hit the same database. To be resilient under load, it's important to not overload the database by allowing too many requests through. In a messaging system, this could translate to limiting the number of handlers that can be running at once \(aka concurrency\). In addition to [concurrency configuration](configuration.md#withconcurrencylimit), JustSaying provides a mechanism to group queues so that they share the same concurrency and backpressure.

So, if you have several queues whose handlers all hit the same database, you might want to configure them to use the same subscription group so that you only handle as many messages as the database can support.

By default, each queue has its own SubscriptionGroup.

#### By Example

In this scenario, the `orderreadyevent` and `orderplacedevent` queues share the same subscription group. If a downstream resource such as a database becomes overloaded, then as the handlers slow down, backpressure is applied to the `IMessageReceiveBuffer` for each queue, which will slow down their reading from SQS. 

In addition, both queues share the same concurrency limit, meaning that if the concurrency limit is set to 10, then the combined maximum number of concurrently handled messages across both queues would be 10.

![](../../.gitbook/assets/image%20%282%29.png)





