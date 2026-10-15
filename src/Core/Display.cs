using System;

namespace HideBar.Core
{
    class Display
    {
        public IntPtr Handle { get; set; }
        public string DeviceName { get; set; }  // \\.\DISPLAY3
        public string ModelId { get; set; }     // DWE2100, from the EDID; null when Windows reports none
        public string Name { get; set; }        // ASUS VP249, from the EDID; null when it has none
        public bool IsPrimary { get; set; }
        public Rect Bounds { get; set; }
        public Rect WorkArea { get; set; }

        // Filled in by TaskbarController.GetDisplays.
        public int Number { get; set; }         // 1, 2, 3... from left to right
        public bool TaskbarVisible { get; set; }

        // The main display's taskbar is the one Explorer depends on, so it is never hidden.
        public bool CanHideTaskbar
        {
            get { return !IsPrimary && ModelId != null; }
        }
    }
}
