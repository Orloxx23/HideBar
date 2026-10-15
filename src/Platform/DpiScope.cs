using System;

namespace HideBar.Platform
{
    // Makes the current thread per-monitor DPI aware until disposed. The process itself is only
    // system DPI aware, which keeps WinForms crisp, but monitor and taskbar coordinates have to
    // be physical pixels or the work area comes out wrong on monitors with different scaling.
    sealed class DpiScope : IDisposable
    {
        readonly IntPtr previous;

        public DpiScope()
        {
            try
            {
                previous = NativeMethods.SetThreadDpiAwarenessContext(NativeMethods.DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            }
            catch (EntryPointNotFoundException)
            {
                // Windows 10 before version 1607 has no per-thread awareness.
            }
        }

        public void Dispose()
        {
            if (previous != IntPtr.Zero) NativeMethods.SetThreadDpiAwarenessContext(previous);
        }
    }
}
