# Custom Middleware

### Definition

Middleware in JustSaying is similar to middleware in ASP.NET Core. They derive from `MiddlewareBase<HandleMessageContext, bool>`, and implement this method: 

```csharp
Task<bool> RunInnerAsync(
    HandleMessageContext context,
    Func<CancellationToken, Task<bool>> func,
    CancellationToken stoppingToken)
```

`HandleMessageContext:` A context object containing the Message, its type, and the queue it was pulled from.

`Func<CancellationToken, Task<bool>>`: The next middleware in the pipeline that you should call in your implementation.

`CancellationToken`: This is the top level CancellationToken that is passed to `StartAsync` - it should be checked and passed to operations that accept one so that if the bus stops, any in-progress middlewares can be cancelled.



### Usage

Once you've defined a middleware, it can be added to a pipeline using the `WithMiddlewareConfiguration` API with the `Use<TMiddleware>` method. In this case, the `TMiddleware` will be resolved from the provided `IServiceResolver`.

Note that all middlewares are singletons, and will only be resolved once per pipeline.

Alternatively, you can also expose an extension method to make it more easily discoverable to users, or to parameterise it. To do this, ensure the following:

1. The namespace of the extension method is `JustSaying.Messaging.Middleware`, which will ensure the method appears in autocomplete for all users. 
2. It is of the form `public static HandlerMiddlewareBuilder UseXX(this HandlerMiddlewareBuilder builder)`
   1. Essentially, its an extension method on `HandlerMiddlewareBuilder`, which is the builder exposed in the API. You may accept additional parameters here that can be passed to the middleware. 
3. If you require services from the DI container to create or resolve the middleware, resolve them from `builder.ServiceResolver`. 
   1. Note that you may also need to register required services into the container for them to be made available here.

