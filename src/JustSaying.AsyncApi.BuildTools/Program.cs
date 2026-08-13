using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Hosting;

namespace JustSaying.AsyncApi.GetDocument;

/// <summary>
/// Entry point for build-time AsyncAPI document generation. The MSBuild targets in the
/// JustSaying.AsyncApi.BuildTools package invoke this tool with
/// <c>dotnet exec --depsfile {app}.deps.json --runtimeconfig {app}.runtimeconfig.json</c>,
/// so the application's assemblies resolve exactly as if the application itself were running.
/// The tool captures the application's host as it is built (the host is never started, so no
/// hosted services run and no AWS calls are made), resolves JustSaying's document provider
/// from the container by reflection, and writes each document to the output directory.
/// </summary>
internal static class Program
{
    private const string ProviderTypeName = "JustSaying.AsyncApi.IAsyncApiDocumentProvider";
    private const string ProviderAssemblyName = "JustSaying.AsyncApi";
    private const string DefaultDocumentName = "asyncapi";
    private const string TimeoutPropertyName = "JustSayingAsyncApiEntryPointTimeoutSeconds";

    private static readonly TimeSpan GenerationTimeout = TimeSpan.FromMinutes(2);

    public static int Main(string[] args)
    {
        string? assemblyPath = null;
        string? outputDirectory = null;
        string? fileListPath = null;
        string? fileName = null;
        string? entryPointTimeout = null;

        for (var i = 0; i < args.Length - 1; i++)
        {
            switch (args[i])
            {
                case "--assembly":
                    assemblyPath = args[++i];
                    break;
                case "--output":
                    outputDirectory = args[++i];
                    break;
                case "--file-list":
                    fileListPath = args[++i];
                    break;
                case "--file-name":
                    fileName = args[++i];
                    break;
                case "--entry-point-timeout":
                    entryPointTimeout = args[++i];
                    break;
            }
        }

        if (assemblyPath is null || outputDirectory is null)
        {
            return Error(1, "Usage: JustSaying.AsyncApi.GetDocument --assembly <path> --output <directory> [--file-list <path>] [--file-name <name>] [--entry-point-timeout <seconds>]");
        }

        if (fileName is not null && !Regex.IsMatch(fileName, "^[A-Za-z0-9_.-]+$"))
        {
            return Error(1, $"The file name '{fileName}' is invalid. File names must contain only letters, digits, '.', '-' and '_'.");
        }

        TimeSpan? waitTimeout = null;
        if (entryPointTimeout is not null)
        {
            if (!uint.TryParse(entryPointTimeout, out var timeoutSeconds) || timeoutSeconds == 0)
            {
                return Error(1, $"The entry point timeout '{entryPointTimeout}' is invalid. Specify a positive number of seconds.");
            }

            waitTimeout = TimeSpan.FromSeconds(timeoutSeconds);
        }

        Assembly assembly;
        try
        {
            assembly = Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(assemblyPath)));
        }
        catch (Exception exception)
        {
            return Error(2, $"Failed to load the application assembly '{assemblyPath}': {exception.Message}");
        }

        var serviceProviderFactory = HostFactoryResolver.ResolveServiceProviderFactory(assembly, waitTimeout);
        if (serviceProviderFactory is null)
        {
            return Error(3,
                $"The entry point of '{assembly.GetName().Name}' does not build a host, so its services cannot be discovered. " +
                "Build-time AsyncAPI generation requires the application to use Host.CreateApplicationBuilder, WebApplication.CreateBuilder, " +
                "or a static CreateHostBuilder(string[]) method on the Program class.");
        }

        IServiceProvider? services;
        try
        {
            services = serviceProviderFactory([]);
        }
        catch (Exception exception)
        {
            var reason = (exception as TargetInvocationException)?.InnerException ?? exception;

            if (reason is InvalidOperationException && reason.Message.StartsWith("Timed out waiting for the entry point", StringComparison.Ordinal))
            {
                return Error(4,
                    $"The entry point of '{assembly.GetName().Name}' did not build its host within {(waitTimeout ?? TimeSpan.FromMinutes(5)).TotalSeconds:0} seconds. " +
                    $"If the application does slow work before building the host, raise the {TimeoutPropertyName} MSBuild property, " +
                    "or skip that work during generation (the entry assembly is 'JustSaying.AsyncApi.GetDocument' while a document is generated).");
            }

            return Error(4,
                $"The entry point of '{assembly.GetName().Name}' threw while building the host: {reason.Message} " +
                "The application's Program runs during document generation (its host is built but never started); " +
                "it must be able to reach the host Build() call at build time. " +
                "Note that the entry point is invoked with the arguments ['--applicationName', '<assembly name>'], " +
                "and that the entry assembly is 'JustSaying.AsyncApi.GetDocument' while a document is generated.");
        }

        if (services is null)
        {
            return Error(4, $"The entry point of '{assembly.GetName().Name}' built a host, but no service provider could be obtained from it.");
        }

        var providerType = FindProviderType();
        if (providerType is null)
        {
            return Error(5,
                $"'{assembly.GetName().Name}' does not reference the {ProviderAssemblyName} package, which provides the document generator. " +
                $"Add a reference to {ProviderAssemblyName} and call AddJustSayingAsyncApi() when configuring services.");
        }

        var provider = services.GetService(providerType);
        if (provider is null)
        {
            return Error(6,
                "No AsyncAPI document provider is registered with the application's service provider. " +
                "Call AddJustSayingAsyncApi() when configuring the application's services.");
        }

        var getDocumentNames = providerType.GetMethod("GetDocumentNames", Type.EmptyTypes);
        var generateAsync = providerType.GetMethod("GenerateAsync", [typeof(string), typeof(TextWriter), typeof(CancellationToken)]);
        if (getDocumentNames is null || generateAsync is null || !typeof(IEnumerable<string>).IsAssignableFrom(getDocumentNames.ReturnType))
        {
            return Error(7,
                $"The {ProviderTypeName} contract in the application's version of {ProviderAssemblyName} does not match this tool. " +
                $"Update the {ProviderAssemblyName} and JustSaying.AsyncApi.BuildTools packages to the same version.");
        }

        try
        {
            return GenerateDocuments(provider, getDocumentNames, generateAsync, outputDirectory, fileListPath, fileName);
        }
        catch (Exception exception)
        {
            var reason = exception is AggregateException aggregate ? aggregate.Flatten().InnerException ?? exception : exception;
            return Error(8, $"Generating the AsyncAPI document failed: {reason.Message}");
        }
    }

    private static int GenerateDocuments(
        object provider,
        MethodInfo getDocumentNames,
        MethodInfo generateAsync,
        string outputDirectory,
        string? fileListPath,
        string? fileName)
    {
        Directory.CreateDirectory(outputDirectory);

        var generatedFiles = new List<string>();
        foreach (var documentName in (IEnumerable<string>)getDocumentNames.Invoke(provider, null)!)
        {
            var filePath = Path.Combine(outputDirectory, GetDocumentFileName(documentName, fileName));

            using var writer = new StringWriter();
            var task = (Task)generateAsync.Invoke(provider, [documentName, writer, CancellationToken.None])!;
            if (!task.Wait(GenerationTimeout))
            {
                return Error(9, $"Generating the AsyncAPI document '{documentName}' did not complete within {GenerationTimeout.TotalSeconds:0} seconds.");
            }

            // Leave the file untouched when the content has not changed so that its timestamp
            // stays stable and a committed document does not show up as modified.
            var content = writer.ToString();
            if (!File.Exists(filePath) || !string.Equals(File.ReadAllText(filePath), content, StringComparison.Ordinal))
            {
                File.WriteAllText(filePath, content);
                Console.WriteLine($"Generated AsyncAPI document '{documentName}' -> {filePath}");
            }
            else
            {
                Console.WriteLine($"AsyncAPI document '{documentName}' is up to date: {filePath}");
            }

            generatedFiles.Add(filePath);
        }

        if (fileListPath is not null)
        {
            var fileListDirectory = Path.GetDirectoryName(Path.GetFullPath(fileListPath));
            if (!string.IsNullOrEmpty(fileListDirectory))
            {
                Directory.CreateDirectory(fileListDirectory);
            }

            File.WriteAllLines(fileListPath, generatedFiles);
        }

        return 0;
    }

    private static Type? FindProviderType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (assembly.GetType(ProviderTypeName, throwOnError: false) is { } type)
            {
                return type;
            }
        }

        // The assembly may be referenced but not yet loaded if the application never called
        // AddJustSayingAsyncApi(); loading it here distinguishes "not registered" (a better
        // error) from "not referenced".
        try
        {
            return Assembly.Load(ProviderAssemblyName).GetType(ProviderTypeName, throwOnError: false);
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Names the output file. The default document keeps the configured file name alone
    /// (<c>asyncapi.json</c>, or <c>MyProject.json</c> when the file name is overridden);
    /// any additional documents are suffixed with the document name, mirroring
    /// Microsoft.Extensions.ApiDescription.Server's naming.
    /// </summary>
    private static string GetDocumentFileName(string documentName, string? fileName)
    {
        fileName ??= DefaultDocumentName;
        return string.Equals(documentName, DefaultDocumentName, StringComparison.Ordinal)
            ? fileName + ".json"
            : $"{fileName}_{SanitizeFileName(documentName)}.json";
    }

    private static string SanitizeFileName(string documentName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Create(documentName.Length, documentName, (buffer, name) =>
        {
            for (var i = 0; i < name.Length; i++)
            {
                buffer[i] = Array.IndexOf(invalid, name[i]) >= 0 ? '_' : name[i];
            }
        });
    }

    private static int Error(int exitCode, string message)
    {
        // The canonical MSBuild error format, so that Exec surfaces the message as a build error.
        Console.Error.WriteLine($"JustSaying.AsyncApi.GetDocument : error JSAA{exitCode:000}: {message}");
        return exitCode;
    }
}
