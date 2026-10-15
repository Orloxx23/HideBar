using System;
using System.Collections.Generic;

namespace HideBar.Core
{
    class AppSettings
    {
        public AppSettings()
        {
            MonitorIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            ReclaimSpace = true;
            ShowTrayIcon = true;
        }

        // Model IDs of the displays whose taskbar is hidden.
        public HashSet<string> MonitorIds { get; private set; }

        // Let maximized windows use the space the hidden taskbar leaves.
        public bool ReclaimSpace { get; set; }

        public bool ShowTrayIcon { get; set; }
    }
}
