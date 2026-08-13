using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace JustSaying.AsyncApi.Tests;

/// <summary>
/// Exercises the JustSaying.AsyncApi.GetDocument tool end to end, invoking it the same way the
/// JustSaying.AsyncApi.BuildTools MSBuild targets do: <c>dotnet exec</c> with the application's
/// deps.json and runtimeconfig.json, against a real compiled application
/// (JustSaying.AsyncApi.Tests.App).
/// </summary>
public class WhenGeneratingAtBuildTime
{
    private sealed record ToolResult(int ExitCode, string StandardOutput, string StandardError);

    [Test]
    public async Task TheDocumentIsWrittenToTheOutputDirectory()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(outputDirectory.Path);

        if (result.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"The tool exited with code {result.ExitCode}.{Environment.NewLine}{result.StandardError}{Environment.NewLine}{result.StandardOutput}");
        }

        var documentPath = Path.Combine(outputDirectory.Path, "asyncapi.json");
        await Assert.That(File.Exists(documentPath)).IsTrue();

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(documentPath));
        await Assert.That(document.RootElement.GetProperty("info").GetProperty("title").GetString()).IsEqualTo("Test App");
        await Assert.That(document.RootElement.GetProperty("channels").TryGetProperty("orderreadyevent", out _)).IsTrue();

        var fileList = await File.ReadAllLinesAsync(Path.Combine(outputDirectory.Path, "files.txt"));
        await Assert.That(fileList).Contains(documentPath);
    }

    [Test]
    public async Task AnUnchangedDocumentIsNotRewritten()
    {
        using var outputDirectory = new TemporaryDirectory();
        var documentPath = Path.Combine(outputDirectory.Path, "asyncapi.json");

        var firstRun = await RunTool(outputDirectory.Path);
        await Assert.That(firstRun.ExitCode).IsEqualTo(0);
        var writtenAt = File.GetLastWriteTimeUtc(documentPath);

        var secondRun = await RunTool(outputDirectory.Path);

        await Assert.That(secondRun.ExitCode).IsEqualTo(0);
        await Assert.That(secondRun.StandardOutput).Contains("is up to date");
        await Assert.That(File.GetLastWriteTimeUtc(documentPath)).IsEqualTo(writtenAt);
    }

    [Test]
    public async Task TheFileNameCanBeOverridden()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(outputDirectory.Path, extraArguments: ["--file-name", "My.Sample-App"]);

        await Assert.That(result.ExitCode).IsEqualTo(0);
        var documentPath = Path.Combine(outputDirectory.Path, "My.Sample-App.json");
        await Assert.That(File.Exists(documentPath)).IsTrue();

        var fileList = await File.ReadAllLinesAsync(Path.Combine(outputDirectory.Path, "files.txt"));
        await Assert.That(fileList).Contains(documentPath);
    }

    [Test]
    public async Task AnInvalidFileNameFailsWithAnError()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(outputDirectory.Path, extraArguments: ["--file-name", "not/a valid?name"]);

        await Assert.That(result.ExitCode).IsNotEqualTo(0);
        await Assert.That(result.StandardError).Contains("error JSAA");
        await Assert.That(result.StandardError).Contains("file name");
    }

    [Test]
    public async Task ASlowEntryPointFailsFastWithTheConfiguredTimeout()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(
            outputDirectory.Path,
            extraArguments: ["--entry-point-timeout", "2"],
            ("JUSTSAYING_TESTAPP_BLOCK_STARTUP", "1"));

        await Assert.That(result.ExitCode).IsNotEqualTo(0);
        await Assert.That(result.StandardError).Contains("error JSAA");
        await Assert.That(result.StandardError).Contains("JustSayingAsyncApiEntryPointTimeoutSeconds");
    }

    [Test]
    public async Task AMissingHandlerFailsWithAnErrorExplainingWhatToRegister()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(outputDirectory.Path, ("JUSTSAYING_TESTAPP_SKIP_HANDLER", "1"));

        await Assert.That(result.ExitCode).IsNotEqualTo(0);
        await Assert.That(result.StandardError).Contains("error JSAA");
        await Assert.That(result.StandardError).Contains("AddJustSayingHandler");
    }

    [Test]
    public async Task AMissingAsyncApiRegistrationFailsWithAnErrorNamingTheRegistrationMethod()
    {
        using var outputDirectory = new TemporaryDirectory();

        var result = await RunTool(outputDirectory.Path, ("JUSTSAYING_TESTAPP_SKIP_ASYNCAPI", "1"));

        await Assert.That(result.ExitCode).IsNotEqualTo(0);
        await Assert.That(result.StandardError).Contains("error JSAA");
        await Assert.That(result.StandardError).Contains("AddJustSayingAsyncApi");
    }

    private static Task<ToolResult> RunTool(string outputDirectory, params (string Name, string Value)[] environment)
        => RunTool(outputDirectory, extraArguments: [], environment);

    private static async Task<ToolResult> RunTool(
        string outputDirectory,
        string[] extraArguments,
        params (string Name, string Value)[] environment)
    {
        var appDirectory = GetAssemblyMetadata("TestAppDirectory");
        var toolPath = GetAssemblyMetadata("GetDocumentToolPath");
        var appPath = Path.Combine(appDirectory, "JustSaying.AsyncApi.Tests.App.dll");

        var startInfo = new ProcessStartInfo()
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            WorkingDirectory = appDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            ArgumentList =
            {
                "exec",
                "--depsfile",
                Path.Combine(appDirectory, "JustSaying.AsyncApi.Tests.App.deps.json"),
                "--runtimeconfig",
                Path.Combine(appDirectory, "JustSaying.AsyncApi.Tests.App.runtimeconfig.json"),
                toolPath,
                "--assembly",
                appPath,
                "--output",
                outputDirectory,
                "--file-list",
                Path.Combine(outputDirectory, "files.txt"),
            },
        };

        foreach (var argument in extraArguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        foreach (var (name, value) in environment)
        {
            startInfo.Environment[name] = value;
        }

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the tool process.");
        var standardOutput = process.StandardOutput.ReadToEndAsync();
        var standardError = process.StandardError.ReadToEndAsync();

        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The tool did not exit within three minutes.");
        }

        return new ToolResult(process.ExitCode, await standardOutput, await standardError);
    }

    private static string GetAssemblyMetadata(string key)
    {
        return typeof(WhenGeneratingAtBuildTime).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single((attribute) => attribute.Key == key)
            .Value;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "justsaying-asyncapi-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException)
            {
                // Best effort; the OS cleans the temp directory eventually.
            }
        }
    }
}
