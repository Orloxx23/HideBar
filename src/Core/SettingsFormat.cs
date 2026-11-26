using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HideBar.Core
{
    // The settings.ini format:
    //
    //   [monitors]
    //   DWE2100
    //   [options]
    //   reclaimSpace=true
    //   keepBottomMargin=false
    //   showTrayIcon=true
    //   [bottomMargins]
    //   <monitor device path>=16
    static class SettingsFormat
    {
        public static AppSettings Parse(IEnumerable<string> lines)
        {
            var settings = new AppSettings();
            string section = "";
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (IsBlankOrComment(line)) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                    continue;
                }

                if (section == "monitors") settings.MonitorIds.Add(line);
                else if (section == "options") ReadOption(settings, line);
                else if (section == "bottommargins") ReadBottomMargin(settings, line);
            }
            return settings;
        }

        // Version 1.0 kept only the monitor list, in monitors.txt, one model ID per line.
        public static AppSettings ParseLegacy(IEnumerable<string> lines)
        {
            var settings = new AppSettings();
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (!IsBlankOrComment(line)) settings.MonitorIds.Add(line);
            }
            return settings;
        }

        public static string Format(AppSettings settings)
        {
            var monitorIds = new List<string>(settings.MonitorIds);
            monitorIds.Sort(StringComparer.OrdinalIgnoreCase);

            var text = new StringBuilder();
            text.AppendLine("; HideBar settings");
            text.AppendLine("[monitors]");
            foreach (string id in monitorIds) text.AppendLine(id);
            text.AppendLine("[options]");
            text.AppendLine("reclaimSpace=" + FormatBool(settings.ReclaimSpace));
            text.AppendLine("keepBottomMargin=" + FormatBool(settings.KeepBottomMargin));
            text.AppendLine("showTrayIcon=" + FormatBool(settings.ShowTrayIcon));
            if (settings.BottomMargins.Count > 0)
            {
                var devicePaths = new List<string>(settings.BottomMargins.Keys);
                devicePaths.Sort(StringComparer.OrdinalIgnoreCase);
                text.AppendLine("[bottomMargins]");
                foreach (string path in devicePaths)
                    text.AppendLine(path + "=" + settings.BottomMargins[path].ToString(CultureInfo.InvariantCulture));
            }
            return text.ToString();
        }

        static void ReadBottomMargin(AppSettings settings, string line)
        {
            int equals = line.LastIndexOf('=');
            if (equals <= 0) return;
            int pixels;
            if (int.TryParse(line.Substring(equals + 1).Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out pixels)
                && pixels <= AppSettings.MaxBottomMarginPixels)
                settings.BottomMargins[line.Substring(0, equals).Trim()] = pixels;
        }

        static void ReadOption(AppSettings settings, string line)
        {
            int equals = line.IndexOf('=');
            if (equals < 0) return;

            string key = line.Substring(0, equals).Trim().ToLowerInvariant();
            string value = line.Substring(equals + 1).Trim().ToLowerInvariant();
            bool enabled = value == "true" || value == "1" || value == "yes";

            if (key == "reclaimspace") settings.ReclaimSpace = enabled;
            else if (key == "keepbottommargin") settings.KeepBottomMargin = enabled;
            else if (key == "showtrayicon") settings.ShowTrayIcon = enabled;
        }

        static bool IsBlankOrComment(string line)
        {
            return line.Length == 0 || line.StartsWith(";") || line.StartsWith("#");
        }

        static string FormatBool(bool value)
        {
            return value ? "true" : "false";
        }
    }
}
