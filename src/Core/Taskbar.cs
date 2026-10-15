using System;

namespace HideBar.Core
{
    class Taskbar
    {
        public IntPtr Handle { get; set; }
        public IntPtr Monitor { get; set; }     // the display it sits on
        public Rect Bounds { get; set; }
        public bool IsVisible { get; set; }
        public bool IsPrimary { get; set; }     // the main display's taskbar
    }
}
