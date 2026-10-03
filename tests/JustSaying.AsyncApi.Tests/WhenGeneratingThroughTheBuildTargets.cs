using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// Builds a real consumer project through the JustSaying.AsyncApi.BuildTools MSBuild targets, the
/// way an application referencing the package does. The consumer lives outside the repository so
/// it gets the SDK's default layout (relative <c>obj/</c> and <c>bin/</c> directories) rather than
/// this repository's artifacts layout, whose absolute paths hide path-resolution mistakes. It
/// references the assemblies of JustSaying.AsyncApi.Tests.App rather than packages, so building it
/// needs nothing from the network.
/// </summary>
public class WhenGeneratingThroughTheBuildTargets
{
    private const string GenerationTarget = "_GenerateJustSayingAsyncApiDocuments";

    private sealed record BuildResult(int ExitCode, string Output);

    [Test]
    public async Task ANoOpBuildSkipsGeneration()
    {
        using var project = ConsumerProject.Create();

        var first = await project.BuildAsync();
        await AssertSucceeded(first);
        await Assert.That(File.Exists(project.DocumentPath)).IsTrue();

        // The file list is in the project's own obj/ directory, not resolved against bin/.
        await Assert.That(File.Exists(Path.Combine(project.Directory, "obj", "Debug", "net8.0", "JustSayingAsyncApi.cache"))).IsTrue();

        var second = await project.BuildAsync();
        await AssertSucceeded(second);
        await Assert.That(second.Output).Contains($"Skipping target \"{GenerationTarget}\" because all output files are up-to-date");
    }

    [Test]
    public async Task GenerationWarningsAreBuildWarnings()
    {
        using var project = ConsumerProject.Create();

        var result = await project.BuildAsync();

        await AssertSucceeded(result);

        // MSBuild logs a warning with the project it came from appended; the tool's own output is not.
        var warnings = result.Output.Split('\n')
            .Where((line) => line.Contains("JustSaying.AsyncApi.GetDocument : warning JSAA102: Publication of OrderShipped uses a dynamic destination name", StringComparison.Ordinal))
            .ToList();
        await Assert.That(warnings).Contains((line) => line.TrimEnd().EndsWith("Consumer.csproj]", StringComparison.Ordinal));
    }

    [Test]
    public async Task ADeletedDocumentIsGeneratedAgain()
    {
        using var project = ConsumerProject.Create();
        await AssertSucceeded(await project.BuildAsync());

        File.Delete(project.DocumentPath);
        var result = await project.BuildAsync();

        await AssertSucceeded(result);
        await Assert.That(result.Output).Contains($"Building target \"{GenerationTarget}\" completely");
        await Assert.That(File.Exists(project.DocumentPath)).IsTrue();
    }

    [Test]
    public async Task ChangedApplicationSettingsGenerateTheDocumentAgain()
    {
        using var project = ConsumerProject.Create();
        await AssertSucceeded(await project.BuildAsync());

        // The host reads appsettings*.json as it is built, so they can change what is configured.
        await File.WriteAllTextAsync(Path.Combine(project.Directory, "appsettings.json"), "{}");
        var result = await project.BuildAsync();

        await AssertSucceeded(result);
        await Assert.That(result.Output).Contains($"Building target \"{GenerationTarget}\"");
        await Assert.That(result.Output).DoesNotContain($"Skipping target \"{GenerationTarget}\"");
    }

    [Test]
    public async Task BuildingForAnotherRuntimeSkipsGenerationWithAMessage()
    {
        using var project = ConsumerProject.Create();
        var otherRuntime = RuntimeInformation.RuntimeIdentifier == "linux-x64" ? "linux-arm64" : "linux-x64";

        var result = await project.BuildAsync("-r", otherRuntime);

        await AssertSucceeded(result);
        await Assert.That(result.Output).Contains("Skipping AsyncAPI document generation");
        await Assert.That(result.Output).Contains("JustSayingAsyncApiGenerateDocumentsOnBuild");
        await Assert.That(File.Exists(project.DocumentPath)).IsFalse();
    }

    [Test]
    public async Task PublishingDoesNotGenerateTheDocument()
    {
        using var project = ConsumerProject.Create();

        var result = await project.RunAsync("publish", []);

        await AssertSucceeded(result);
        await Assert.That(result.Output).DoesNotContain($"Target \"{GenerationTarget}\"");
        await Assert.That(File.Exists(project.DocumentPath)).IsFalse();
    }

    [Test]
    public async Task TheToolRunsOnTheDotNetHostRunningTheBuild()
    {
        using var project = ConsumerProject.Create();

        // The build is started by absolute path, with no dotnet on the PATH at all.
        var result = await project.RunAsync("build", [], removeDotNetFromPath: true);

        await AssertSucceeded(result);
        await Assert.That(File.Exists(project.DocumentPath)).IsTrue();
    }

    [Test]
    public async Task TheHostIsBuiltInTheConfiguredEnvironment()
    {
        using var project = ConsumerProject.Create();

        // The application can only build its host with configuration from appsettings.Development.json.
        var programPath = Path.Combine(project.Directory, "Program.cs");
        await File.WriteAllTextAsync(programPath, (await File.ReadAllTextAsync(programPath)).Replace(
            "var builder = Host.CreateApplicationBuilder(args);",
            """
            var builder = Host.CreateApplicationBuilder(args);
            _ = builder.Configuration["AuditTopicArn"] ?? throw new InvalidOperationException("AuditTopicArn is not configured.");
            """,
            StringComparison.Ordinal));
        await File.WriteAllTextAsync(Path.Combine(project.Directory, "appsettings.Development.json"), """{ "AuditTopicArn": "arn:aws:sns:eu-west-1:000000000000:audit" }""");

        var production = await project.BuildAsync();

        // The tool's error names the environment.
        await Assert.That(production.ExitCode).IsNotEqualTo(0);
        await Assert.That(production.Output).Contains("error JSAA004: The entry point of 'Consumer' threw while building the host in the 'Production' environment");
        await Assert.That(production.Output).Contains("JustSayingAsyncApiEnvironment");

        var development = await project.BuildAsync("-p:JustSayingAsyncApiEnvironment=Development");

        await AssertSucceeded(development);
        await Assert.That(File.Exists(project.DocumentPath)).IsTrue();

        // A document generated for another environment is out of date, though nothing else changed.
        var productionAgain = await project.BuildAsync();

        await Assert.That(productionAgain.ExitCode).IsNotEqualTo(0);
        await Assert.That(productionAgain.Output).Contains("error JSAA004");
    }

    [Test]
    public async Task TheWebSdkDoesNotPublishTheDocumentUnlessAskedTo()
    {
        using var project = ConsumerProject.Create("Microsoft.NET.Sdk.Web");
        await File.WriteAllTextAsync(project.DocumentPath, "{}");

        await Assert.That(await project.GetDocumentCopyToPublishDirectoryAsync()).IsEqualTo("Never");
        await Assert.That(await project.GetDocumentCopyToPublishDirectoryAsync("-p:JustSayingAsyncApiCopyDocumentsToPublishDirectory=true")).IsEqualTo("PreserveNewest");
    }

    private static async Task AssertSucceeded(BuildResult result)
    {
        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException($"The build exited with code {result.ExitCode}.{Environment.NewLine}{result.Output}");
        }

        await Assert.That(result.ExitCode).IsEqualTo(0);
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(WhenGeneratingThroughTheBuildTargets).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single((attribute) => attribute.Key == key)
            .Value;
    }

    private sealed class ConsumerProject : IDisposable
    {
        private const string ProgramSource =
            """
            using JustSaying.Messaging.MessageHandling;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            var builder = Host.CreateApplicationBuilder(args);

            builder.Services.AddJustSaying(config =>
            {
                config.Messaging(x => x.WithRegion("eu-west-1"));
                config.Publications(x =>
                {
                    x.WithTopic<OrderReady>();

                    // A destination computed per message can't be documented, which is a generation warning.
                    x.WithTopic<OrderShipped>(t => t.WithTopicName(m => $"shipped-{m.OrderId % 4}"));
                });
                config.Subscriptions(x => x.ForTopic<OrderPlaced>());
            });
            builder.Services.AddJustSayingHandler<OrderPlaced, OrderPlacedHandler>();
            builder.Services.AddJustSayingAsyncApi();

            builder.Build();

            public record OrderPlaced(int OrderId);

            public record OrderReady(int OrderId);

            public record OrderShipped(int OrderId);

            public class OrderPlacedHandler : IHandlerAsync<OrderPlaced>
            {
                public Task<bool> Handle(OrderPlaced message) => Task.FromResult(true);
            }
            """;

        private ConsumerProject(string directory)
        {
            Directory = directory;
        }

        public string Directory { get; }

        public string DocumentPath => Path.Combine(Directory, "asyncapi.json");

        public static ConsumerProject Create(string sdk = "Microsoft.NET.Sdk")
        {
            var directory = Path.Combine(Path.GetTempPath(), "justsaying-asyncapi-consumer-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(directory);

            var buildToolsDirectory = GetAssemblyMetadata("BuildToolsMSBuildDirectory");
            var appDirectory = GetAssemblyMetadata("TestAppDirectory");

            // Imported where NuGet imports a package's build files: the props before the project's
            // own properties, the targets after the SDK's.
            var projectFile =
                $"""
                <Project>
                  <Import Project="Sdk.props" Sdk="{sdk}" />
                  <Import Project="{Path.Combine(buildToolsDirectory, "JustSaying.AsyncApi.BuildTools.props")}" />
                  <PropertyGroup>
                    <OutputType>Exe</OutputType>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                    <UseAppHost>false</UseAppHost>
                  </PropertyGroup>
                  <ItemGroup>
                    <None Update="appsettings*.json" CopyToOutputDirectory="PreserveNewest" />
                    <Reference Include="{appDirectory}JustSaying*.dll;{appDirectory}AWSSDK*.dll;{appDirectory}ByteBard*.dll;{appDirectory}Microsoft.Extensions.*.dll;{appDirectory}System.*.dll"
                               Exclude="{appDirectory}JustSaying.AsyncApi.Tests.App.dll" />
                  </ItemGroup>
                  <Import Project="Sdk.targets" Sdk="{sdk}" />
                  <Import Project="{Path.Combine(buildToolsDirectory, "JustSaying.AsyncApi.BuildTools.targets")}" />
                </Project>
                """;

            File.WriteAllText(Path.Combine(directory, "Consumer.csproj"), projectFile);
            File.WriteAllText(Path.Combine(directory, "Program.cs"), ProgramSource);

            // Build with the repository's SDK rather than whichever is newest on the machine.
            File.Copy(GetAssemblyMetadata("GlobalJsonPath"), Path.Combine(directory, "global.json"));

            return new ConsumerProject(directory);
        }

        public Task<BuildResult> BuildAsync(params string[] extraArguments)
            => RunAsync("build", extraArguments);

        public async Task<string> GetDocumentCopyToPublishDirectoryAsync(params string[] extraArguments)
        {
            var result = await RunAsync("msbuild", ["-getItem:Content", .. extraArguments]);
            await AssertSucceeded(result);

            using var items = JsonDocument.Parse(result.Output.Substring(result.Output.IndexOf('{', StringComparison.Ordinal)));
            return items.RootElement.GetProperty("Items").GetProperty("Content").EnumerateArray()
                .Single((item) => item.GetProperty("Identity").GetString() == "asyncapi.json")
                .GetProperty("CopyToPublishDirectory").GetString();
        }

        public async Task<BuildResult> RunAsync(string command, string[] extraArguments, bool removeDotNetFromPath = false)
        {
            var startInfo = new ProcessStartInfo()
            {
                FileName = DotNetHostPath(),
                WorkingDirectory = Directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList =
                {
                    command,
                    "Consumer.csproj",
                    "-v:d",
                    "-nodeReuse:false",
                    "-p:UseSharedCompilation=false",

                    // The targets find the tool in the package; here it is the one this repository built.
                    $"-p:_JustSayingAsyncApiToolPath={GetAssemblyMetadata("GetDocumentToolPath")}",
                },
            };

            foreach (var argument in extraArguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            // Don't inherit the state of the build that launched the tests (for example an SDK path),
            // and leave no build servers running that would hold the output pipes open.
            foreach (var name in startInfo.Environment.Keys.Where((key) => key.StartsWith("MSBuild", StringComparison.OrdinalIgnoreCase)).ToList())
            {
                startInfo.Environment.Remove(name);
            }

            if (removeDotNetFromPath)
            {
                startInfo.Environment["PATH"] = string.Join(
                    Path.PathSeparator,
                    (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                        .Split(Path.PathSeparator)
                        .Where((directory) => !File.Exists(Path.Combine(directory, "dotnet")) && !File.Exists(Path.Combine(directory, "dotnet.exe"))));
            }

            startInfo.Environment["MSBUILDDISABLENODEREUSE"] = "1";
            startInfo.Environment["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0";

            using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the build.");
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();

            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new TimeoutException("The build did not exit within five minutes.");
            }

            return new BuildResult(process.ExitCode, await standardOutput + await standardError);
        }

        private static string DotNetHostPath()
        {
            if (Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") is { Length: > 0 } hostPath)
            {
                return hostPath;
            }

            return (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                .Split(Path.PathSeparator)
                .SelectMany((directory) => new[] { Path.Combine(directory, "dotnet"), Path.Combine(directory, "dotnet.exe") })
                .FirstOrDefault(File.Exists) ?? "dotnet";
        }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the OS cleans the temp directory eventually.
            }
        }
    }
}
