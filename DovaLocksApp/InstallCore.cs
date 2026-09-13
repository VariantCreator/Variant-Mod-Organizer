using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace DovaInstaller;

public record GameLocation(string Root, string Label)
{
    public override string ToString() => Label;
}
public record InstallPlan(string Root, string Mods, IReadOnlyList<string> Previous);
public record InstallResult(string Destination, string Backup, int Replaced);
public record ModFile(string Path, long Length, DateTime Modified)
{
    public string Name => System.IO.Path.GetFileName(Path);
    public string Size => $"{Length / 1048576d:0.##} MB";
}
public record RemovalResult(string Backup, int Removed);

public static class InstallCore
{
    public const string Release = "1.1.0";
    public const string PayloadHash = "fde1fda04ee1ca3273bfc462999d465439d28e9dd89d023850f9e0d6a7b85e84";
    public const string FileName = "Dova-Locks_P.pak";
    // Exact release names, not a broad 'Dova*' wildcard. Other mods are untouched.
    private static readonly Regex PreviousName = new(
        @"^(?:Dova-Locks_P|dovaslock_v(?:4[a-d]?|5(?:[a-p]|k1|l1|n1)?)(?:_P)?|DovaLocks_ClientTrace_v3_P|DovaLocks_PakCompatibility_v[12]_P)\.pak$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static string? NormalizeGame(string selection)
    {
        if (string.IsNullOrWhiteSpace(selection)) return null;
        string full;
        try { full = Path.GetFullPath(selection); } catch { return null; }
        var candidates = new List<string> { full, Path.Combine(full, "Icarus") };
        var cursor = new DirectoryInfo(full);
        for (int i = 0; i < 3 && cursor.Parent != null; i++) { cursor = cursor.Parent; candidates.Add(cursor.FullName); }
        foreach (var root in candidates.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            string content = Path.Combine(root, "Content");
            string bins = Path.Combine(root, "Binaries", "Win64");
            if (!File.Exists(Path.Combine(content, "Data", "data.pak")) || !Directory.Exists(Path.Combine(content, "Paks")) || !Directory.Exists(bins)) continue;
            if (Directory.EnumerateFiles(bins, "Icarus*.exe").Any()) return Path.TrimEndingDirectorySeparator(root);
        }
        return null;
    }

    public static IReadOnlyList<GameLocation> Detect()
    {
        var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        void Add(string? path) { if (!string.IsNullOrWhiteSpace(path)) try { libraries.Add(Path.GetFullPath(path)); } catch { } }
        Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"));
        try { Add(Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string); } catch { }
        try { Add(Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath", null) as string); } catch { }
        foreach (string steam in libraries.ToArray())
        {
            string vdf = Path.Combine(steam, "steamapps", "libraryfolders.vdf");
            try
            {
                if (File.Exists(vdf)) foreach (Match match in Regex.Matches(File.ReadAllText(vdf), "\"path\"\\s+\"([^\"]+)\"")) Add(match.Groups[1].Value.Replace(@"\\", @"\"));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        var found = new Dictionary<string, GameLocation>(StringComparer.OrdinalIgnoreCase);
        void Find(string path)
        {
            try
            {
                string? root = NormalizeGame(path);
                if (root != null) found[root] = new(root, (Directory.EnumerateFiles(Path.Combine(root, "Binaries", "Win64"), "*Server*.exe").Any() ? "Icarus Dedicated Server" : "Icarus") + "  ·  " + Path.GetPathRoot(root));
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        foreach (string library in libraries)
        {
            string apps = Path.Combine(library, "steamapps");
            Find(Path.Combine(apps, "common", "Icarus"));
            if (!Directory.Exists(apps)) continue;
            try
            {
                foreach (string manifest in Directory.EnumerateFiles(apps, "appmanifest_*.acf"))
                {
                    string text = File.ReadAllText(manifest);
                    var name = Regex.Match(text, "\"name\"\\s+\"([^\"]+)\"");
                    var dir = Regex.Match(text, "\"installdir\"\\s+\"([^\"]+)\"");
                    if (name.Success && dir.Success && name.Groups[1].Value.Contains("Icarus", StringComparison.OrdinalIgnoreCase)) Find(Path.Combine(apps, "common", dir.Groups[1].Value));
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
        return found.Values.OrderBy(x => x.Root, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static void CheckTree(string root, string path)
    {
        root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(root));
        path = Path.GetFullPath(path);
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("The destination must remain inside the selected Icarus folder.");
        for (var item = new DirectoryInfo(path); item != null; item = item.Parent)
        {
            if (item.Exists && (item.Attributes & FileAttributes.ReparsePoint) != 0) throw new IOException("This install uses a linked folder. Select the actual Icarus folder instead of a junction or symbolic link.");
            if (item.FullName.Equals(root, StringComparison.OrdinalIgnoreCase)) break;
        }
    }

    public static InstallPlan Plan(string selection)
    {
        string root = NormalizeGame(selection) ?? throw new IOException("Choose the Icarus game or dedicated-server folder. It must contain the game's Content and Binaries folders.");
        string paks = Path.Combine(root, "Content", "Paks");
        string mods = Directory.EnumerateDirectories(paks).FirstOrDefault(d => Path.GetFileName(d).Equals("mods", StringComparison.OrdinalIgnoreCase)) ?? Path.Combine(paks, "mods");
        CheckTree(root, mods);
        CheckTree(root, Path.Combine(root, "DovaLocks-Backups"));
        // Older builds may also have been placed directly in Paks or ~mods.
        var dirs = new List<string> { paks, mods };
        dirs.AddRange(Directory.EnumerateDirectories(paks).Where(d => Path.GetFileName(d).Equals("~mods", StringComparison.OrdinalIgnoreCase)));
        var previous = new List<string>();
        foreach (string dir in dirs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            CheckTree(root, dir);
            if (!Directory.Exists(dir)) continue;
            foreach (string file in Directory.EnumerateFiles(dir).Where(f => PreviousName.IsMatch(Path.GetFileName(f))))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0) throw new IOException("A Dova PAK is a symbolic link. Replace the linked file manually before installing.");
                previous.Add(file);
            }
        }
        return new(root, mods, previous.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray());
    }

    public static void EnsureGameClosed(string root)
    {
        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                try
                {
                    if (!process.ProcessName.StartsWith("Icarus", StringComparison.OrdinalIgnoreCase)) continue;
                    string? executable = process.MainModule?.FileName;
                    if (executable == null || executable.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new IOException("Close Icarus and its dedicated server for this installation, then click Install / Update again.");
                }
                catch (System.ComponentModel.Win32Exception) { throw new IOException("Close Icarus and its dedicated server before installing, then try again."); }
                catch (InvalidOperationException) { /* Process exited while being checked. */ }
            }
        }
    }

    public static Stream Payload() => Assembly.GetExecutingAssembly().GetManifestResourceStream("Dova.Payload.pak") ?? throw new IOException("The installer is missing its mod file.");
    public static IReadOnlyList<ModFile> ScanMods(string selection)
    {
        var plan = Plan(selection);
        if (!Directory.Exists(plan.Mods)) return Array.Empty<ModFile>();
        return Directory.EnumerateFiles(plan.Mods).Where(p => Path.GetExtension(p).Equals(".pak", StringComparison.OrdinalIgnoreCase))
            .Select(p => new FileInfo(p)).Where(f => (f.Attributes & FileAttributes.ReparsePoint) == 0)
            .Select(f => new ModFile(f.FullName, f.Length, f.LastWriteTimeUtc)).OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static RemovalResult RemoveMods(string selection, IReadOnlyList<ModFile> selected,
        Action<string>? closedCheck = null, Action<int>? checkpoint = null)
    {
        if (selected.Count == 0) throw new IOException("Select at least one mod first.");
        var plan = Plan(selection);
        (closedCheck ?? EnsureGameClosed)(plan.Root);
        var unique = selected.DistinctBy(f => f.Path, StringComparer.OrdinalIgnoreCase).ToArray();
        var backups = Path.Combine(plan.Root, "DovaLocks-Backups");
        Directory.CreateDirectory(backups);
        using var guard = new FileStream(Path.Combine(backups, ".install-lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        foreach (var mod in unique)
        {
            var full = Path.GetFullPath(mod.Path);
            if (!string.Equals(Path.GetDirectoryName(full), plan.Mods, StringComparison.OrdinalIgnoreCase) ||
                !Path.GetExtension(full).Equals(".pak", StringComparison.OrdinalIgnoreCase)) throw new IOException("Only PAK files in this Mods folder can be removed.");
            var info = new FileInfo(full);
            if (!info.Exists || (info.Attributes & FileAttributes.ReparsePoint) != 0 || info.Length != mod.Length || info.LastWriteTimeUtc != mod.Modified)
                throw new IOException("The Mods folder changed. Scan again before removing files.");
        }
        string backup = Path.Combine(backups, "Removed_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(backup);
        var moved = new List<(string From, string To)>();
        try
        {
            (closedCheck ?? EnsureGameClosed)(plan.Root);
            CheckTree(plan.Root, plan.Mods);
            foreach (var mod in unique)
            {
                if ((File.GetAttributes(mod.Path) & FileAttributes.ReparsePoint) != 0) throw new IOException("The Mods folder changed. Scan again.");
                var copy = Path.Combine(backup, Path.GetFileName(mod.Path));
                File.Move(mod.Path, copy);
                moved.Add((mod.Path, copy));
                checkpoint?.Invoke(moved.Count);
            }
            File.WriteAllText(Path.Combine(backup, "RESTORE.txt"), "To restore these mods, close Icarus and move the PAK files back to:\r\n" + plan.Mods);
            return new(backup, moved.Count);
        }
        catch (Exception error)
        {
            var failures = new List<string>();
            foreach (var pair in moved.AsEnumerable().Reverse())
                try { File.Move(pair.To, pair.From); } catch (Exception ex) { failures.Add(ex.Message); }
            if (failures.Count != 0) throw new IOException("Removal stopped. Restore remaining files from " + backup + ". " + string.Join(" ", failures), error);
            throw;
        }
    }
    public static string Hash(string path) { using var file = File.OpenRead(path); return Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant(); }

    public static InstallResult Install(string selection, IProgress<string>? progress = null) => Apply(selection, Payload, PayloadHash, EnsureGameClosed, progress);

    // Separable core enables rollback testing without touching a live game folder.
    public static InstallResult Apply(string selection, Func<Stream> source, string expectedHash, Action<string> closedCheck,
        IProgress<string>? progress = null, Action<string>? checkpoint = null, string? release = null)
    {
        var plan = Plan(selection);
        closedCheck(plan.Root);
        string backups = Path.Combine(plan.Root, "DovaLocks-Backups");
        Directory.CreateDirectory(backups);
        using var guard = new FileStream(Path.Combine(backups, ".install-lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        string backup = Path.Combine(backups, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + "_" + Guid.NewGuid().ToString("N")[..8]);
        if (Directory.Exists(Path.Combine(plan.Root, "Variant-Mod-Organizer", "Disabled"))) throw new IOException("The game switched to vanilla mode. Click Install / Update again to restore your mods and update.");
        Directory.CreateDirectory(backup);
        string staged = Path.Combine(backup, "payload.tmp");
        string destination = Path.Combine(plan.Mods, FileName);
        var moved = new List<(string Original, string Backup)>();
        bool published = false;
        try
        {
            progress?.Report("Checking the mod file…");
            using (var input = source()) using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None)) { input.CopyTo(output); output.Flush(true); }
            if (!Hash(staged).Equals(expectedHash, StringComparison.OrdinalIgnoreCase)) throw new IOException("The mod file could not be verified. Check for updates and try again.");
            closedCheck(plan.Root);
            var fresh = Plan(selection);
            if (!fresh.Previous.SequenceEqual(plan.Previous, StringComparer.OrdinalIgnoreCase)) throw new IOException("The Mods folder changed. Select it again and retry.");
            Directory.CreateDirectory(plan.Mods);
            progress?.Report("Backing up files being replaced…");
            foreach (string old in plan.Previous)
            {
                string copy = Path.Combine(backup, Path.GetRelativePath(plan.Root, old));
                Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
                File.Move(old, copy);
                moved.Add((old, copy));
            }
            checkpoint?.Invoke("backed-up");
            progress?.Report("Installing Dova Locks…");
            File.Move(staged, destination);
            published = true;
            checkpoint?.Invoke("published");
            if (!Hash(destination).Equals(expectedHash, StringComparison.OrdinalIgnoreCase)) throw new IOException("The installed file did not pass verification.");
            File.WriteAllText(Path.Combine(backup, "installation.json"), JsonSerializer.Serialize(new { release = release ?? Release, installed = destination, sha256 = expectedHash, previous = moved.Select(x => new { original = x.Original, backup = x.Backup }) }, new JsonSerializerOptions { WriteIndented = true }));
            return new(destination, backup, moved.Count);
        }
        catch (Exception error)
        {
            var recoveryErrors = new List<string>();
            if (published)
            {
                try { if (File.Exists(destination)) File.Move(destination, Path.Combine(backup, "failed-new-payload.bin")); }
                catch (Exception recovery) { recoveryErrors.Add(recovery.Message); }
            }
            foreach (var pair in moved.AsEnumerable().Reverse())
            {
                try { File.Move(pair.Backup, pair.Original); }
                catch (Exception recovery) { recoveryErrors.Add(recovery.Message); }
            }
            if (recoveryErrors.Count > 0) throw new IOException("Installation stopped. Some previous files need restoring from " + backup + ". " + string.Join(" ", recoveryErrors), error);
            throw;
        }
        finally { if (File.Exists(staged)) File.Delete(staged); }
    }
}
