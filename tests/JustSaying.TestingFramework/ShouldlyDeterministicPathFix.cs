#nullable enable
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Shouldly;
using Shouldly.Configuration;

namespace JustSaying.TestingFramework;

#pragma warning disable CA2255
internal static class ShouldlyDeterministicPathFix
{
    private static IDisposable? _sourceDisabler;

    [ModuleInitializer]
    internal static void Initialize()
    {
        // Disable source code reading in errors — it fails with deterministic paths
        // and is not needed for CI. Keep reference to prevent disposal.
        _sourceDisabler = ShouldlyConfiguration.DisableSourceInErrors();

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

        sourceDir = FixDeterministicPath(sourceDir);

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

    private static string FixDeterministicPath(string path)
    {
        if (!path.StartsWith("/_/", StringComparison.Ordinal))
            return path;

        var repoRoot = GetRepoRoot();
        if (repoRoot != null)
        {
            return repoRoot + path.Substring(3);
        }

        return path;
    }

    private static string? GetRepoRoot()
    {
        // Walk up from the base directory to find the repo root (contains global.json)
        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
                return dir.EndsWith(Path.DirectorySeparatorChar)
                    ? dir
                    : dir + Path.DirectorySeparatorChar;
            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}
#pragma warning restore CA2255
