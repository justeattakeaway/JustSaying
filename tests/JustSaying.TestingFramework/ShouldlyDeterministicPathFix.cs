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
            .Configure(c => c.TestMethodFinder = new DeterministicPathTestMethodFinder());
    }

    internal static string FixDeterministicPath(string path)
    {
        // Handle both forward-slash (Linux/macOS PDB) and backslash (Windows normalized) prefixes
        if (!path.StartsWith("/_/", StringComparison.Ordinal) &&
            !path.StartsWith("\\_\\", StringComparison.Ordinal))
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

    private static readonly FieldInfo? SourceFileField =
        typeof(TestMethodInfo).GetField("<SourceFileName>k__BackingField",
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
            FixDeterministicPaths(info);
            return info;
        }

        return new TestMethodInfo(stackTrace.GetFrame(startAt)!);
    }

    private static void FixDeterministicPaths(TestMethodInfo info)
    {
        if (IsDeterministicPath(info.SourceFileDirectory))
        {
            var fixedDir = ShouldlyDeterministicPathFix.FixDeterministicPath(info.SourceFileDirectory!);
            SourceFileDirField?.SetValue(info, fixedDir);
        }

        var sourceFile = (string?)SourceFileField?.GetValue(info);
        if (IsDeterministicPath(sourceFile))
        {
            var fixedFile = ShouldlyDeterministicPathFix.FixDeterministicPath(sourceFile!);
            SourceFileField?.SetValue(info, fixedFile);
        }
    }

    private static bool IsDeterministicPath(string? path) =>
        path != null &&
        (path.StartsWith("/_/", StringComparison.Ordinal) ||
         path.StartsWith("\\_\\", StringComparison.Ordinal));
}
#pragma warning restore CA2255
