using System;

namespace HideBar.Platform
{
    // Windows 10 and 11 window chrome that WinForms doesn't expose. Older systems ignore it.
    static class WindowStyling
    {
        public static void UseDarkTitleBar(IntPtr window, bool dark)
        {
            SetAttribute(window, NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE, dark ? 1 : 0);
        }

        public static void UseRoundedCorners(IntPtr window)
        {
            SetAttribute(window, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, NativeMethods.DWMWCP_ROUND);
        }

        static void SetAttribute(IntPtr window, int attribute, int value)
        {
            try
            {
                NativeMethods.DwmSetWindowAttribute(window, attribute, ref value, sizeof(int));
            }
            catch (DllNotFoundException)
            {
            }
            catch (EntryPointNotFoundException)
            {
            }
        }
    }
}
