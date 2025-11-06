---
description: AKA Error Queues
---

# Dead Letter Queues

JustSaying supports error queues and this option is enabled by default. When a handler is unable to handle a message, JustSaying will attempt to re-deliver the message up to 5 times \(handler retry count is configurable\) and if the handler is still unable to handle the message then the message will be moved to an error queue. You can opt out [during subscription configuration](configuration/sqsreadconfiguration.md#withnoerrorqueue).

