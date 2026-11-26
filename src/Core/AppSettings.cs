using System;
using System.Collections.Generic;

namespace HideBar.Core
{
    class AppSettings
    {
        public const int MaxBottomMarginPixels = 128;

        public AppSettings()
        {
            MonitorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            BottomMargins = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            ReclaimSpace = true;
            ShowTrayIcon = true;
        }

        // Model IDs of the displays whose taskbar is hidden.
        public HashSet<string> MonitorIds { get; private set; }

        // Let maximized windows use the space the hidden taskbar leaves.
        public bool ReclaimSpace { get; set; }
        public bool KeepBottomMargin { get; set; }
        // Custom margins by monitor device path. A saved zero overrides the default.
        public Dictionary<string, int> BottomMargins { get; private set; }

        public bool ShowTrayIcon { get; set; }
    }
}
