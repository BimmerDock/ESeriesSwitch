using System.IO;
using System.Text.Json;

namespace ESeriesSwitch.Services
{
    public enum DesktopLocation
    {
        /// <summary>The user's own desktop.</summary>
        User,
        /// <summary>The Public desktop (shortcuts installed for all users).</summary>
        Public
    }

    public sealed record ShortcutItem(string Key, DesktopLocation Location, string FileName, bool IsHidden, string? Target)
    {
        public string Name => Path.GetFileNameWithoutExtension(FileName);
    }

    public sealed record ShortcutSyncResult(int Hidden, int Restored, IReadOnlyList<string> Skipped, IReadOnlyList<string> Errors);

    /// <summary>
    /// Hides the selected desktop shortcuts while ISTA+ is active, so the desktop itself shows which mode is on.
    /// Nothing is deleted: shortcuts are moved to C:\ProgramData\ESeriesSwitch\HiddenShortcuts and moved back
    /// to exactly where they came from, including the icon's position on the desktop. A manifest records every move.
    /// </summary>
    public static class DesktopShortcuts
    {
        static readonly string[] Extensions = [".lnk", ".url"];

        // Suggested for hiding: the E-series tools (installed under EC-APPS, or the classic EDIABAS / INPA / NCS / NFS folders)
        static readonly string[] SuggestedTargetParts = [@"\EC-APPS\", @"\INPA\", @"\NCSEXPER\", @"\NFS\", @"\EDIABAS\"];
        static readonly string[] SuggestedNameParts = ["inpa", "tool32", "ncs", "winkfp", "nfs", "ediabas"];
        // Never suggested: needed in ISTA+ mode as well
        static readonly string[] NeverSuggestedParts = ["ista", "radar"];

        public static string StoreDir { get; } = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ESeriesSwitch", "HiddenShortcuts");

        static string ManifestPath => Path.Combine(StoreDir, "manifest.json");

        // X / Y: icon position on the desktop when it was hidden (null if it could not be read)
        sealed record Entry(DesktopLocation Location, string FileName, string StoredFile, int? X = null, int? Y = null);

        public static string KeyOf(DesktopLocation location, string fileName) => $"{location}|{fileName}";

        static string DesktopDir(DesktopLocation location) => Environment.GetFolderPath(
            location == DesktopLocation.User
                ? Environment.SpecialFolder.DesktopDirectory
                : Environment.SpecialFolder.CommonDesktopDirectory);

        /// <summary>All shortcuts on both desktops, plus the ones currently hidden by the app.</summary>
        public static List<ShortcutItem> List()
        {
            var items = new List<ShortcutItem>();
            foreach (var location in Enum.GetValues<DesktopLocation>())
            {
                var dir = DesktopDir(location);
                if (!Directory.Exists(dir))
                    continue;
                foreach (var path in Directory.EnumerateFiles(dir).Where(IsShortcut))
                {
                    var fileName = Path.GetFileName(path);
                    items.Add(new ShortcutItem(KeyOf(location, fileName), location, fileName, false, ReadTarget(path)));
                }
            }

            foreach (var entry in LoadManifest())
            {
                var key = KeyOf(entry.Location, entry.FileName);
                if (items.Any(i => i.Key.Equals(key, StringComparison.OrdinalIgnoreCase)))
                    continue;
                items.Add(new ShortcutItem(key, entry.Location, entry.FileName, true,
                    ReadTarget(Path.Combine(StoreDir, entry.StoredFile))));
            }

            // Suggested (E-series) tools first, then alphabetically
            return items
                .OrderByDescending(IsSuggested)
                .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        }

        public static bool IsSuggested(ShortcutItem item)
        {
            var name = item.Name;
            var target = item.Target ?? "";
            if (NeverSuggestedParts.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase)
                                             || target.Contains(p, StringComparison.OrdinalIgnoreCase)))
                return false;
            return SuggestedTargetParts.Any(p => target.Contains(p, StringComparison.OrdinalIgnoreCase))
                || SuggestedNameParts.Any(p => name.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Brings the desktop in line with the wanted state: when <paramref name="hide"/> is true, the selected
        /// shortcuts are moved away; every other shortcut moved away earlier is put back.
        /// </summary>
        public static ShortcutSyncResult Sync(bool hide, IReadOnlyCollection<string> selectedKeys)
        {
            var selected = new HashSet<string>(selectedKeys, StringComparer.OrdinalIgnoreCase);
            var manifest = LoadManifest();
            int hidden = 0, restored = 0, dropped = 0;
            var skipped = new List<string>();
            var errors = new List<string>();
            var positionsToRestore = new Dictionary<string, (int X, int Y)>(StringComparer.OrdinalIgnoreCase);

            // Put back everything that should be visible
            foreach (var entry in manifest.ToList())
            {
                if (hide && selected.Contains(KeyOf(entry.Location, entry.FileName)))
                    continue;

                var source = Path.Combine(StoreDir, entry.StoredFile);
                var destination = Path.Combine(DesktopDir(entry.Location), entry.FileName);
                try
                {
                    if (!File.Exists(source))
                    {
                        manifest.Remove(entry); // gone from the store: nothing left to restore
                        dropped++;
                        continue;
                    }
                    if (File.Exists(destination))
                    {
                        skipped.Add(entry.FileName); // never overwrite a shortcut that is on the desktop again
                        continue;
                    }
                    File.Move(source, destination);
                    manifest.Remove(entry);
                    restored++;
                    if (entry.X is int x && entry.Y is int y)
                        positionsToRestore[entry.FileName] = (x, y);
                }
                catch (Exception ex)
                {
                    errors.Add($"{entry.FileName}: {ex.Message}");
                }
            }

            // Windows puts restored icons into the first free slot, so move them back to where they were
            DesktopIconPositions.TrySetPositions(positionsToRestore, TimeSpan.FromSeconds(3));

            // Move away the selected ones, remembering where their icons are
            if (hide)
            {
                Dictionary<string, (int X, int Y)>? positions = null;
                foreach (var key in selected)
                {
                    var parts = key.Split('|', 2);
                    if (parts.Length != 2 || !Enum.TryParse<DesktopLocation>(parts[0], out var location))
                        continue;
                    var source = Path.Combine(DesktopDir(location), parts[1]);
                    if (!File.Exists(source))
                        continue;

                    try
                    {
                        positions ??= DesktopIconPositions.TryGetPositions() ?? [];
                        (int X, int Y)? position = positions.TryGetValue(parts[1], out var p) ? p : null;

                        Directory.CreateDirectory(StoreDir);
                        var stored = UniqueStoreName(location, parts[1]);
                        File.Move(source, Path.Combine(StoreDir, stored));
                        manifest.Add(new Entry(location, parts[1], stored, position?.X, position?.Y));
                        hidden++;
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"{parts[1]}: {ex.Message}");
                    }
                }
            }

            // Only write the manifest when something actually moved
            if (hidden + restored + dropped > 0)
                SaveManifest(manifest);
            return new ShortcutSyncResult(hidden, restored, skipped, errors);
        }

        static bool IsShortcut(string path) =>
            Extensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

        static string UniqueStoreName(DesktopLocation location, string fileName)
        {
            var name = $"{location}_{fileName}";
            for (int i = 2; File.Exists(Path.Combine(StoreDir, name)); i++)
                name = $"{location}_{Path.GetFileNameWithoutExtension(fileName)} ({i}){Path.GetExtension(fileName)}";
            return name;
        }

        /// <summary>Target of a .lnk file (via the Windows Script Host), null for .url files or on failure.</summary>
        static string? ReadTarget(string path)
        {
            if (!path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
                return null;
            try
            {
                var shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null)
                    return null;
                dynamic shell = Activator.CreateInstance(shellType)!;
                return (string)shell.CreateShortcut(path).TargetPath;
            }
            catch
            {
                return null;
            }
        }

        static List<Entry> LoadManifest()
        {
            try
            {
                if (File.Exists(ManifestPath))
                    return JsonSerializer.Deserialize<List<Entry>>(File.ReadAllText(ManifestPath)) ?? [];
            }
            catch
            {
                // An unreadable manifest is treated as empty; the stored files stay in the folder
            }
            return [];
        }

        static void SaveManifest(List<Entry> manifest)
        {
            if (manifest.Count == 0 && !File.Exists(ManifestPath))
                return;
            Directory.CreateDirectory(StoreDir);
            File.WriteAllText(ManifestPath, JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
