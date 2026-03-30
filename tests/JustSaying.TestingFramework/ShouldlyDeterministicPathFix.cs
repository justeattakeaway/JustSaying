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
    [ModuleInitializer]
    internal static void Initialize()
    {
        ShouldlyConfiguration.ShouldMatchApprovedDefaults
            .Configure(c => c.TestMethodFinder = new DeterministicPathTestMethodFinder())
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

    internal static string FixDeterministicPath(string path)
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

    private static string? _cachedRepoRoot;

    private static string? GetRepoRoot()
    {
        if (_cachedRepoRoot != null)
            return _cachedRepoRoot;

        var dir = AppContext.BaseDirectory;
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir, "global.json")))
            {
                _cachedRepoRoot = dir.EndsWith(Path.DirectorySeparatorChar)
                    ? dir
                    : dir + Path.DirectorySeparatorChar;
                return _cachedRepoRoot;
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }
}

internal sealed class DeterministicPathTestMethodFinder : ITestMethodFinder
{
    private static readonly FieldInfo? SourceFileDirField =
        typeof(TestMethodInfo).GetField("<SourceFileDirectory>k__BackingField",
            BindingFlags.NonPublic | BindingFlags.Instance);

    public TestMethodInfo GetTestMethodInfo(StackTrace stackTrace, int startAt)
    {
        for (var i = startAt; i < stackTrace.FrameCount; i++)
        {
            var frame = stackTrace.GetFrame(i);
            if (frame == null) continue;

            var method = frame.GetMethod();
            if (method == null || method.DeclaringType == null) continue;

            // Skip Shouldly internals
            var ns = method.DeclaringType.Namespace;
            if (ns != null && ns.StartsWith("Shouldly", StringComparison.Ordinal))
                continue;

            var info = new TestMethodInfo(frame);

            // Fix deterministic source paths
            if (info.SourceFileDirectory?.StartsWith("/_/", StringComparison.Ordinal) == true)
            {
                var fixedDir = ShouldlyDeterministicPathFix.FixDeterministicPath(info.SourceFileDirectory);
                SourceFileDirField?.SetValue(info, fixedDir);
            }

            return info;
        }

        return new TestMethodInfo(stackTrace.GetFrame(startAt)!);
    }
}
#pragma warning restore CA2255
