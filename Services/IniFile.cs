using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using ESeriesSwitch.Localization;

namespace ESeriesSwitch.Services
{
    /// <summary>
    /// Minimal, byte-exact INI editing: reads or replaces the value of one existing "key = value" line
    /// inside one section. Nothing else in the file is touched, and no lines are ever added or removed.
    /// </summary>
    public static class IniFile
    {
        // Latin1 round-trips every byte, so the rest of the file (accents, tabs, line endings) stays untouched.
        static readonly Encoding FileEncoding = Encoding.Latin1;

        /// <summary>The trimmed value, or null if the file, the section or the key does not exist.</summary>
        public static string? Read(string path, string section, string key)
        {
            if (!File.Exists(path))
                return null;
            var match = FindValue(Load(path), section, key);
            return match?.Value.Trim();
        }

        /// <summary>Replaces the value of an existing key. Saves a backup copy of the file first.</summary>
        public static void Write(string path, string section, string key, string value)
        {
            var text = Load(path);
            var group = FindValue(text, section, key)
                ?? throw new InvalidOperationException(Loc.T("ErrIniKeyMissing", key, section, Path.GetFileName(path)));

            // One backup per file per second. When one action writes several lines of the same file,
            // the first backup (the original state) is kept and never overwritten.
            Directory.CreateDirectory(EnvironmentSwitcher.BackupDir);
            var backupName = $"{Path.GetFileNameWithoutExtension(path)}_{DateTime.Now:yyyyMMdd_HHmmss}{Path.GetExtension(path)}";
            var backupPath = Path.Combine(EnvironmentSwitcher.BackupDir, backupName);
            if (!File.Exists(backupPath))
                File.Copy(path, backupPath);

            // Keep the whitespace around the value (and anything before a ; comment)
            var old = group.Value;
            // An emptied value becomes "key=" (no dangling space)
            var leading = value.Length == 0 ? "" : old[..(old.Length - old.TrimStart().Length)];
            var trailing = old[old.TrimEnd().Length..];
            var updated = text[..group.Index] + leading + value + trailing + text[(group.Index + group.Length)..];
            File.WriteAllBytes(path, FileEncoding.GetBytes(updated));
        }

        static string Load(string path) => FileEncoding.GetString(File.ReadAllBytes(path));

        static Group? FindValue(string text, string section, string key)
        {
            // "[INSTANZ]" and "[ INSTANZ ]" are both valid
            var sectionMatch = new Regex($@"^\[[ \t]*{Regex.Escape(section)}[ \t]*\][ \t]*\r?$",
                RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(text);
            if (!sectionMatch.Success)
                return null;

            int start = sectionMatch.Index + sectionMatch.Length;
            var next = new Regex(@"^[ \t]*\[", RegexOptions.Multiline).Match(text, start);
            int end = next.Success ? next.Index : text.Length;

            // Commented-out lines (starting with ;) never match
            var keyMatch = new Regex($@"^[ \t]*{Regex.Escape(key)}[ \t]*=(?<value>[^\r\n;]*)",
                RegexOptions.Multiline | RegexOptions.IgnoreCase).Match(text, start);
            return keyMatch.Success && keyMatch.Index < end ? keyMatch.Groups["value"] : null;
        }
    }
}
