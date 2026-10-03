---
slug: /
sidebar_position: 1
---

# Introduction

JustSaying is a .NET library for publishing and consuming messages using SNS/SQS.

It provides the following features:

* A fluent configuration API
* Easy to use publish/subscribe conventions
* A message pump that can provide backpressure to SQS so that multiple workers only consume as much work as they can handle
* An interrogation API to enable easy diagnostics
* Extensible logging, monitoring, middleware, locking API's
* Plain classes and records as messages, serialized with System.Text.Json
* [Multi-type queues](/subscriptions/configuration/multi-type-queues), [CloudEvents](/cloudevents/), [AsyncAPI](/asyncapi/) documents and [Native AOT](/advanced/native-aot) support

Upgrading from v8? Start with [Upgrading to v9](/upgrading-to-v9).



