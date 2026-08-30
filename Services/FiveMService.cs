using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using FiveMCleaner.Models;

namespace FiveMCleaner.Services;

public sealed class FiveMService
{
    private static readonly string[] CacheNames =
    [
        "cache",
        "nui-storage",
        "server-cache",
        "server-cache-priv"
    ];

    public string Root { get; }
    public string FiveMExe => Path.Combine(Root, "FiveM.exe");
    public string AppDir => Path.Combine(Root, "FiveM.app");
    public string DataDir => Path.Combine(AppDir, "data");

    public FiveMService(string root) => Root = Path.GetFullPath(root);
    public static string? ResolveInstallationRoot(params string?[] startDirectories)
    {
        var candidates = new List<string>();

        foreach (var startDirectory in startDirectories)
        {
            if (string.IsNullOrWhiteSpace(startDirectory))
                continue;

            try
            {
                var full = Path.GetFullPath(startDirectory);
                if (!candidates.Contains(full, StringComparer.OrdinalIgnoreCase))
                    candidates.Add(full);
            }
            catch
            {
                // Ungültige Kandidaten werden übersprungen; die Suche wird mit dem nächsten Pfad fortgesetzt.
            }
        }

        foreach (var start in candidates)
        {
            var current = new DirectoryInfo(start);

            for (var depth = 0; current is not null && depth <= 12; depth++, current = current.Parent)
            {
                var root = current.FullName;

                if (File.Exists(Path.Combine(root, "FiveM.exe")) &&
                    Directory.Exists(Path.Combine(root, "FiveM.app")) &&
                    Directory.Exists(Path.Combine(root, "FiveM.app", "data")))
                    return root;
            }
        }

        return null;
    }
    public bool IsValidInstallation(out string reason)
    {
        if (!Directory.Exists(Root)) { reason = "InstallationRootUnavailable"; return false; }
        if (!File.Exists(FiveMExe)) { reason = "FiveMExeMissing"; return false; }
        if (!Directory.Exists(AppDir)) { reason = "AppDirMissing"; return false; }
        if (!Directory.Exists(DataDir)) { reason = "DataDirMissing"; return false; }
        reason = string.Empty;
        return true;
    }
    public bool IsFiveMRunning()
    {
        try { return Process.GetProcessesByName("FiveM").Length > 0; }
        catch { return false; }
    }

    public IReadOnlyList<string> CacheTargets => CacheNames.Select(n => Path.Combine(DataDir, n)).ToArray();
    public string LogsTarget => Path.Combine(AppDir, "logs");
    public string CrashesTarget => Path.Combine(AppDir, "crashes");

    public long GetSize(IEnumerable<string> targets)
    {
        long total = 0;
        foreach (var target in targets)
            total += GetDirectorySize(target);
        return total;
    }
    public async Task<CleanupResult> CleanAsync(IEnumerable<string> targets, CancellationToken token = default, Action<int, int, string>? progress = null)
    {
        if (!IsValidInstallation(out var reason))
            return new(false, 0, 0, 0, reason);

        if (IsFiveMRunning())
            return new(false, 0, 0, 0, "CleanupBlockedByRunning");

        var allowed = new HashSet<string>(CacheTargets, StringComparer.OrdinalIgnoreCase)
        {
            LogsTarget,
            CrashesTarget
        };

        long bytes = 0;
        long files = 0;
        var targetList = targets.ToArray();
        int processed = 0;
        int total = targetList.Length;

        foreach (var raw in targetList)
        {
            token.ThrowIfCancellationRequested();
            var target = Path.GetFullPath(raw);
            progress?.Invoke(processed, total, target);

            if (!allowed.Contains(target))
                return new(false, bytes, files, processed, "UnauthorizedTarget");

            if (target.Equals(Root, StringComparison.OrdinalIgnoreCase) ||
                target.Equals(AppDir, StringComparison.OrdinalIgnoreCase) ||
                target.Equals(DataDir, StringComparison.OrdinalIgnoreCase))
                return new(false, bytes, files, processed, "ProtectedTarget");

            if (IsFiveMRunning())
                return new(false, bytes, files, processed, "FiveMStartedDuringCleanup");

            if (Directory.Exists(target) && IsReparsePoint(target))
                return new(false, bytes, files, processed, "ReparseTarget");

            if (!Directory.Exists(target))
            {
                Directory.CreateDirectory(target);
                processed++;
                continue;
            }

            var stats = GetDirectoryStats(target);
            bytes += stats.Bytes;
            files += stats.Files;
            Directory.Delete(target, true);
            Directory.CreateDirectory(target);
            processed++;
            progress?.Invoke(processed, total, target);
            await Task.Yield();
        }

        return new(true, bytes, files, processed, "Cleanup erfolgreich abgeschlossen.");
    }

    private static bool IsReparsePoint(string path)
    {
        try
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return true;
        }
    }

    private static (long Bytes, long Files) GetDirectoryStats(string path)
    {
        if (!Directory.Exists(path)) return (0, 0);

        long bytes = 0;
        long files = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                files++;
                try { bytes += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }

        return (bytes, files);
    }

    private static long GetDirectorySize(string path)
    {
        if (!Directory.Exists(path)) return 0;
        long total = 0;
        try
        {
            foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
            {
                try { total += new FileInfo(file).Length; } catch { }
            }
        }
        catch { }
        return total;
    }
}
