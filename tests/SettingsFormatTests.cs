using HideBar.Core;

namespace HideBar.Tests
{
    static class SettingsFormatTests
    {
        public static void Register()
        {
            TestRunner.Add("settings survive a save and load", RoundTrip);
            TestRunner.Add("an empty file gives the defaults", EmptyFileGivesDefaults);
            TestRunner.Add("unknown keys and comments are ignored", IgnoresUnknownLines);
            TestRunner.Add("reads the monitor list from version 1.0", ReadsLegacyMonitorList);
        }

        static void RoundTrip()
        {
            var settings = new AppSettings();
            settings.MonitorIds.Add("DWE2100");
            settings.MonitorIds.Add("GSM5C9E");
            settings.ReclaimSpace = false;
            settings.ShowTrayIcon = false;

            AppSettings loaded = SettingsFormat.Parse(Lines(SettingsFormat.Format(settings)));

            Assert.Equal(2, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.MonitorIds.Contains("DWE2100"), "DWE2100");
            Assert.True(loaded.MonitorIds.Contains("gsm5c9e"), "GSM5C9E, matched without case");
            Assert.False(loaded.ReclaimSpace, "reclaimSpace");
            Assert.False(loaded.ShowTrayIcon, "showTrayIcon");
        }

        static void EmptyFileGivesDefaults()
        {
            AppSettings loaded = SettingsFormat.Parse(new string[0]);

            Assert.Equal(0, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.ReclaimSpace, "reclaimSpace");
            Assert.True(loaded.ShowTrayIcon, "showTrayIcon");
        }

        static void IgnoresUnknownLines()
        {
            AppSettings loaded = SettingsFormat.Parse(new[]
            {
                "; comment", "[Monitors]", "  DWE2100  ", "", "[colors]", "accent=blue",
                "[OPTIONS]", "ReclaimSpace = no", "volume=11"
            });

            Assert.Equal(1, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.MonitorIds.Contains("DWE2100"), "DWE2100");
            Assert.False(loaded.ReclaimSpace, "reclaimSpace");
            Assert.True(loaded.ShowTrayIcon, "showTrayIcon");
        }

        static void ReadsLegacyMonitorList()
        {
            AppSettings loaded = SettingsFormat.ParseLegacy(new[] { "# picked monitors", "DWE2100", "", "GSM5C9E" });

            Assert.Equal(2, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.MonitorIds.Contains("GSM5C9E"), "GSM5C9E");
        }

        static string[] Lines(string text)
        {
            return text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        }
    }
}
