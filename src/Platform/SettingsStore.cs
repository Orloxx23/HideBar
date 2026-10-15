using System;
using System.IO;
using HideBar.Core;

namespace HideBar.Platform
{
    // Keeps the settings in %APPDATA%\HideBar\settings.ini.
    class SettingsStore : ISettingsStore
    {
        readonly string filePath;
        readonly string legacyFilePath;

        public SettingsStore(string folder)
        {
            filePath = Path.Combine(folder, "settings.ini");
            legacyFilePath = Path.Combine(folder, "monitors.txt");
        }

        public static string DefaultFolder
        {
            get
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appData, "HideBar");
            }
        }

        public AppSettings Load()
        {
            if (File.Exists(filePath)) return SettingsFormat.Parse(File.ReadAllLines(filePath));
            if (!File.Exists(legacyFilePath)) return new AppSettings();

            // Move the monitor list over from version 1.0.
            AppSettings migrated = SettingsFormat.ParseLegacy(File.ReadAllLines(legacyFilePath));
            Save(migrated);
            File.Delete(legacyFilePath);
            return migrated;
        }

        public void Save(AppSettings settings)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, SettingsFormat.Format(settings));
        }
    }
}
