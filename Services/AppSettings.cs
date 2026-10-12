using System.Globalization;
using System.IO;
using System.Text.Json;
using ESeriesSwitch.Localization;

namespace ESeriesSwitch.Services
{
    public sealed class SettingsData
    {
        public string? Language { get; set; }

        /// <summary>Hide the selected desktop shortcuts while ISTA+ mode is active.</summary>
        public bool HideShortcuts { get; set; }

        /// <summary>Selected shortcuts, as keys from <see cref="DesktopShortcuts.KeyOf"/>.</summary>
        public List<string> ShortcutKeys { get; set; } = [];

        /// <summary>Whether the shortcut list is expanded. Expanded until the user collapses it.</summary>
        public bool ShortcutListExpanded { get; set; } = true;
    }

    /// <summary>
    /// App preferences, stored next to the backups in the app's own folder:
    /// C:\ProgramData\ESeriesSwitch\settings.json
    /// </summary>
    public static class AppSettings
    {
        static string FilePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "ESeriesSwitch", "settings.json");

        public static SettingsData Load()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonSerializer.Deserialize<SettingsData>(File.ReadAllText(FilePath)) ?? new SettingsData();
            }
            catch
            {
                // A corrupt settings file must never stop the app from starting
            }
            return new SettingsData();
        }

        public static void Save(SettingsData data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                File.WriteAllText(FilePath, JsonSerializer.Serialize(data));
            }
            catch
            {
                // Not being able to remember a preference is not worth an error dialog
            }
        }

        /// <summary>Saved language, or Hungarian on a Hungarian Windows and English everywhere else.</summary>
        public static AppLanguage LoadLanguage()
        {
            if (Enum.TryParse<AppLanguage>(Load().Language, out var saved))
                return saved;

            return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "hu"
                ? AppLanguage.Hungarian
                : AppLanguage.English;
        }
    }
}
