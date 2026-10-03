---
---

# Build-Time Generation

The `JustSaying.AsyncApi.BuildTools` package writes your application's AsyncAPI document every time it builds, in the same way as ASP.NET Core's build-time OpenAPI generation. Commit the document, and changes to your messaging contract show up in code review.

```bash
dotnet add package JustSaying.AsyncApi
dotnet add package JustSaying.AsyncApi.BuildTools
```

Then call `AddJustSayingAsyncApi()` when configuring services, as for [runtime generation](/asyncapi/). The next `dotnet build` writes `asyncapi.json` next to the project file.

## How it works

After the build, a tool runs your application's entry point up to the point where it builds its host, then generates the document from the host's services. The host is never started, and nothing calls AWS, so no credentials or network access are needed.

The application must build its host with `Host.CreateApplicationBuilder`, `WebApplication.CreateBuilder`, or a static `CreateHostBuilder(string[])` method on its `Program` class. Code before the host is built does run, so anything slow or with side effects there (migrations, waiting for a dependency) needs to be skipped during generation. While a document is generated, the entry assembly is `JustSaying.AsyncApi.GetDocument`:

```csharp
var isGeneratingDocument = Assembly.GetEntryAssembly()?.GetName().Name == "JustSaying.AsyncApi.GetDocument";

if (!isGeneratingDocument)
{
    await MigrateDatabaseAsync();
}
```

The host's environment comes from the build machine's environment variables (`DOTNET_ENVIRONMENT`, `ASPNETCORE_ENVIRONMENT`), not `launchSettings.json`, so it's usually `Production`. Configuration that only exists in development isn't seen.

The document is only rewritten when it changes, and generation is skipped when nothing it depends on (the application, its references, `appsettings*.json` and the project files) has changed since the last build.

Generation runs for executable projects targeting .NET 8 or later. It's skipped for test projects, design-time builds and `dotnet publish`, and when building for a runtime identifier the build machine can't run (for example `linux-arm64` on an x64 agent). A multi-targeted project generates one document, using its first .NET 8+ target framework.

## MSBuild properties

| Property | Default | Description |
| --- | --- | --- |
| `JustSayingAsyncApiGenerateDocumentsOnBuild` | `true` | Set to `false` to stop generating on build. The `GenerateJustSayingAsyncApiDocuments` target can still be run directly: `dotnet msbuild -t:GenerateJustSayingAsyncApiDocuments`. |
| `JustSayingAsyncApiDocumentsDirectory` | The project directory | Where the document is written. A relative path is relative to the project. |
| `JustSayingAsyncApiDocumentFileName` | `asyncapi` | The document's file name, without `.json`. When several projects share a directory, give each its own, for example `$(MSBuildProjectName)`. |
| `JustSayingAsyncApiEntryPointTimeoutSeconds` | `60` | How long to wait for the entry point to build its host before failing the build. |
| `JustSayingAsyncApiCopyDocumentsToPublishDirectory` | `false` | Set to `true` to publish the document with the application. By default it's kept out of the publish output, where the Web SDK would otherwise include it. |

```xml
<PropertyGroup>
  <JustSayingAsyncApiDocumentsDirectory>../../docs/asyncapi</JustSayingAsyncApiDocumentsDirectory>
  <JustSayingAsyncApiDocumentFileName>$(MSBuildProjectName)</JustSayingAsyncApiDocumentFileName>
</PropertyGroup>
```

## Checking the document in CI

Build, then fail if the committed document is out of date:

```bash
dotnet build
git diff --exit-code -- src/Orders.Api/asyncapi.json
```

A change to the messaging contract that wasn't committed fails the build, and the diff shows what changed. Build without a `--runtime` for a runtime the agent can't run, or the document isn't generated.

## Warnings and errors

Anything the generator has to leave out of the document is reported as a build warning:

| Code | Meaning |
| --- | --- |
| JSAA101 | The document is empty: no publications or subscriptions were found. |
| JSAA102 | A publication with a per-message topic name was left out. |
| JSAA103 | A payload schema couldn't be generated for a message type. |
| JSAA104 | Reflection-based serialization is off and the serializer options have no `TypeInfoResolver`, so there are no payload schemas. |
| JSAA105 | A serializer doesn't describe its format, so its messages have no payload schema. |
| JSAA106 | A serializer isn't based on System.Text.Json, so its messages have no payload schema. |
| JSAA107 | Two message types on one topic or queue have the same name, so only one is documented. |

To silence one, add its code to `MSBuildWarningsAsMessages` (or `MSBuildWarningsNotAsErrors` if warnings are errors):

```xml
<PropertyGroup>
  <MSBuildWarningsAsMessages>$(MSBuildWarningsAsMessages);JSAA102</MSBuildWarningsAsMessages>
</PropertyGroup>
```

When generation fails, the build fails with one of these errors:

| Code | Meaning |
| --- | --- |
| JSAA001 | The tool was given an invalid argument, such as an invalid file name or timeout. |
| JSAA002 | The application's assembly couldn't be loaded. |
| JSAA003 | The entry point doesn't build a host. |
| JSAA004 | The entry point threw, or didn't build its host in time. |
| JSAA005 | The application doesn't reference `JustSaying.AsyncApi`. |
| JSAA006 | `AddJustSayingAsyncApi()` isn't called. |
| JSAA007 | The versions of `JustSaying.AsyncApi` and `JustSaying.AsyncApi.BuildTools` don't match. |
| JSAA008 | Generating the document failed, for example because the bus couldn't be built or a handler isn't registered. |
| JSAA009 | Generating the document timed out. |
