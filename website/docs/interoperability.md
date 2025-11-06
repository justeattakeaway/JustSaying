---
description: Message formats and AWS interactions
---

# Interoperability

JustSaying uses a number of Amazon Web Services APIs to transport messages between publishers and subscribers. Since these HTTP APIs can be used by applications which do not use JustSaying, it is entirely possible to have, for example, a Java application publishing a message which is subscribed to by an application using JustSaying. Equally, a C\# application publishing messages using JustSaying, which are subscribed to by a Java application, is fully supported by AWS.

In order to support this interoperability, it is important that the actual message formats are described. Since Amazon provide SDKs for many programming languages and frameworks, you are unlikely to interact with the APIs purely via HTTP, but knowing the structure of the JustSaying message JSON itself is useful for cross-language purposes.

