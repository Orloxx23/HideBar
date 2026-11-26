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
            TestRunner.Add("ignores invalid per-monitor margins", IgnoresInvalidBottomMargins);
        }

        static void RoundTrip()
        {
            var settings = new AppSettings();
            settings.MonitorIds.Add("DWE2100");
            settings.MonitorIds.Add("GSM5C9E");
            settings.ReclaimSpace = false;
            settings.KeepBottomMargin = true;
            settings.BottomMargins[@"\\?\DISPLAY#DWE2100#1"] = 0;
            settings.BottomMargins[@"\\?\DISPLAY#DWE2100#2"] = 16;
            settings.ShowTrayIcon = false;

            AppSettings loaded = SettingsFormat.Parse(Lines(SettingsFormat.Format(settings)));

            Assert.Equal(2, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.MonitorIds.Contains("DWE2100"), "DWE2100");
            Assert.True(loaded.MonitorIds.Contains("gsm5c9e"), "GSM5C9E, matched without case");
            Assert.False(loaded.ReclaimSpace, "reclaimSpace");
            Assert.True(loaded.KeepBottomMargin, "keepBottomMargin");
            Assert.Equal(0, loaded.BottomMargins[@"\\?\display#dwe2100#1"], "explicit zero survives");
            Assert.Equal(16, loaded.BottomMargins[@"\\?\DISPLAY#DWE2100#2"], "independent display margin");
            Assert.False(loaded.ShowTrayIcon, "showTrayIcon");
        }

        static void EmptyFileGivesDefaults()
        {
            AppSettings loaded = SettingsFormat.Parse(new string[0]);

            Assert.Equal(0, loaded.MonitorIds.Count, "monitor count");
            Assert.True(loaded.ReclaimSpace, "reclaimSpace");
            Assert.False(loaded.KeepBottomMargin, "keepBottomMargin");
            Assert.Equal(0, loaded.BottomMargins.Count, "all monitors inherit the default");
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

        static void IgnoresInvalidBottomMargins()
        {
            AppSettings loaded = SettingsFormat.Parse(new[]
            {
                "[bottomMargins]", "A=-1", "B=129", "C=twenty", "D=128"
            });

            Assert.Equal(1, loaded.BottomMargins.Count, "valid margin count");
            Assert.Equal(128, loaded.BottomMargins["D"], "maximum allowed margin");
        }

        static string[] Lines(string text)
        {
            return text.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
        }
    }
}
