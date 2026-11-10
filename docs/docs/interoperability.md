---
description: Message formats and AWS interactions
---

# Interoperability

JustSaying uses a number of Amazon Web Services APIs to transport messages between publishers and subscribers. Since these HTTP APIs can be used by applications which do not use JustSaying, it is entirely possible to have, for example, a Java application publishing a message which is subscribed to by an application using JustSaying. Equally, a C\# application publishing messages using JustSaying, which are subscribed to by a Java application, is fully supported by AWS.

In order to support this interoperability, it is important that the actual message formats are described. Since Amazon provide SDKs for many programming languages and frameworks, you are unlikely to interact with the APIs purely via HTTP, but knowing the structure of the JustSaying message JSON itself is useful for cross-language purposes.

The [How JustSaying uses SQS/SNS](/how-justsaying-uses-sqs-sns) page describes JustSaying's own message format. For other ways to interoperate:

* [CloudEvents](/cloudevents/) publishes and consumes events in the vendor-neutral CloudEvents format, which SDKs for many languages, AWS.Messaging and Brighter can read and write.
* [AsyncAPI](/asyncapi/) documents describe an application's topics, queues and message schemas in a machine-readable form.
* `WithRawMessages()` on a queue publication, and `WithRawMessageDelivery()` on a subscription, send and receive message bodies without JustSaying's wrappers.
* A [custom serializer](/messages/serialization#writing-a-serializer) can read and write any body format.
