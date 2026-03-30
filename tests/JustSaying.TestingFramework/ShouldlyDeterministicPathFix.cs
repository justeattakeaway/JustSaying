using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly.Configuration;

namespace JustSaying.TestingFramework;

internal static class ShouldlyDeterministicPathFix
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults
            .WithFilenameGenerator(ResolveApprovedFilename);
    }

    private static string ResolveApprovedFilename(
        TestMethodInfo testMethodInfo,
        string? discriminator,
        string fileExtension,
        string approvalFileSubFolder)
    {
        var sourceDir = testMethodInfo.SourceFileDirectory ?? string.Empty;

        // On CI with ContinuousIntegrationBuild=true, source paths are mapped to /_/
        // which doesn't exist on disk. Map it back to the actual repo root.
        if (sourceDir.StartsWith("/_/", StringComparison.Ordinal))
        {
            var repoRoot = Assembly.GetCallingAssembly()
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "RepoRoot")?.Value;

            if (repoRoot != null)
            {
                sourceDir = repoRoot + sourceDir.Substring(3);
            }
        }

        if (!string.IsNullOrEmpty(approvalFileSubFolder))
        {
            sourceDir = Path.Combine(sourceDir, approvalFileSubFolder);
        }

        var filename = testMethodInfo.DeclaringTypeName + "." + testMethodInfo.MethodName;
        if (!string.IsNullOrEmpty(discriminator))
        {
            filename += "." + discriminator;
        }

        return Path.Combine(sourceDir, filename + ".approved." + fileExtension);
    }
}
