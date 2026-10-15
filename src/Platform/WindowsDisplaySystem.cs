using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using HideBar.Core;

namespace HideBar.Platform
{
    // The real IDisplaySystem. Coordinates are read and written per-monitor DPI aware.
    class WindowsDisplaySystem : IDisplaySystem
    {
        const string PrimaryTaskbarClass = "Shell_TrayWnd";
        const string SecondaryTaskbarClass = "Shell_SecondaryTrayWnd";

        public List<Display> GetDisplays()
        {
            var displays = new List<Display>();
            Dictionary<string, string> names = ReadMonitorNames();
            using (new DpiScope())
            {
                NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                    delegate (IntPtr monitor, IntPtr hdc, ref NativeMethods.RECT rect, IntPtr data)
                    {
                        Display display = ReadDisplay(monitor);
                        if (display == null) return true;

                        string name;
                        if (names.TryGetValue(display.DeviceName, out name)) display.Name = name;
                        displays.Add(display);
                        return true;
                    },
                    IntPtr.Zero);
            }
            return displays;
        }

        public List<Taskbar> GetTaskbars()
        {
            var taskbars = new List<Taskbar>();
            using (new DpiScope())
            {
                AddTaskbars(taskbars, PrimaryTaskbarClass, true);
                AddTaskbars(taskbars, SecondaryTaskbarClass, false);
            }
            return taskbars;
        }

        public void Hide(Taskbar taskbar)
        {
            NativeMethods.ShowWindow(taskbar.Handle, NativeMethods.SW_HIDE);
        }

        public void Show(Taskbar taskbar)
        {
            NativeMethods.ShowWindow(taskbar.Handle, NativeMethods.SW_SHOWNA);
        }

        public bool IsTaskbarAutoHide()
        {
            var data = new NativeMethods.APPBARDATA();
            data.cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            ulong state = (ulong)NativeMethods.SHAppBarMessage(NativeMethods.ABM_GETSTATE, ref data);
            return (state & NativeMethods.ABS_AUTOHIDE) != 0;
        }

        public void SetWorkArea(Rect area, bool broadcast)
        {
            NativeMethods.RECT rect = ToNative(area);
            uint flags = broadcast ? NativeMethods.SPIF_SENDCHANGE : 0;
            using (new DpiScope())
            {
                NativeMethods.SystemParametersInfo(NativeMethods.SPI_SETWORKAREA, 0, ref rect, flags);
            }
        }

        static Display ReadDisplay(IntPtr monitor)
        {
            var info = new NativeMethods.MONITORINFOEX();
            info.cbSize = Marshal.SizeOf(typeof(NativeMethods.MONITORINFOEX));
            if (!NativeMethods.GetMonitorInfo(monitor, ref info)) return null;

            var display = new Display
            {
                Handle = monitor,
                DeviceName = info.szDevice,
                IsPrimary = (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                Bounds = FromNative(info.rcMonitor),
                WorkArea = FromNative(info.rcWork)
            };
            ReadMonitorDevice(display);
            return display;
        }

        // The monitor's device path looks like \\?\DISPLAY#DWE2100#5&2521ec4&0&UID4356#{...}.
        // Its second part is the model ID from the EDID, which stays the same when the monitor
        // is plugged into another port.
        static void ReadMonitorDevice(Display display)
        {
            var device = new NativeMethods.DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
            uint flags = NativeMethods.EDD_GET_DEVICE_INTERFACE_NAME;

            for (uint i = 0; NativeMethods.EnumDisplayDevices(display.DeviceName, i, ref device, flags); i++)
            {
                string[] parts = (device.DeviceID ?? "").Split('#');
                if (parts.Length > 1)
                {
                    bool isActive = (device.StateFlags & NativeMethods.DISPLAY_DEVICE_ACTIVE) != 0;
                    if (display.ModelId == null || isActive) display.ModelId = parts[1];
                    if (isActive) return;
                }
                device.cb = Marshal.SizeOf(typeof(NativeMethods.DISPLAY_DEVICE));
            }
        }

        // The names in the monitors' EDID ("LG ULTRAFINE"), by GDI device name (\\.\DISPLAY1).
        // Monitors whose EDID has no name are left out.
        static Dictionary<string, string> ReadMonitorNames()
        {
            var names = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            uint pathCount, modeCount;
            uint flags = NativeMethods.QDC_ONLY_ACTIVE_PATHS;
            if (NativeMethods.GetDisplayConfigBufferSizes(flags, out pathCount, out modeCount) != 0) return names;

            var paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];
            if (NativeMethods.QueryDisplayConfig(flags, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0) return names;

            for (int i = 0; i < pathCount; i++)
            {
                var source = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME();
                source.header = Header(NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME, Marshal.SizeOf(source),
                    paths[i].sourceInfo.adapterId, paths[i].sourceInfo.id);

                var target = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME();
                target.header = Header(NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME, Marshal.SizeOf(target),
                    paths[i].targetInfo.adapterId, paths[i].targetInfo.id);

                if (NativeMethods.DisplayConfigGetDeviceInfo(ref source) != 0) continue;
                if (NativeMethods.DisplayConfigGetDeviceInfo(ref target) != 0) continue;
                if (!string.IsNullOrEmpty(target.monitorFriendlyDeviceName))
                    names[source.viewGdiDeviceName] = target.monitorFriendlyDeviceName;
            }
            return names;
        }

        static NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER Header(uint type, int size, NativeMethods.LUID adapter, uint id)
        {
            var header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER();
            header.type = type;
            header.size = (uint)size;
            header.adapterId = adapter;
            header.id = id;
            return header;
        }

        static void AddTaskbars(List<Taskbar> taskbars, string className, bool isPrimary)
        {
            IntPtr window = IntPtr.Zero;
            while ((window = NativeMethods.FindWindowEx(IntPtr.Zero, window, className, null)) != IntPtr.Zero)
            {
                NativeMethods.RECT rect;
                NativeMethods.GetWindowRect(window, out rect);
                taskbars.Add(new Taskbar
                {
                    Handle = window,
                    Monitor = NativeMethods.MonitorFromWindow(window, NativeMethods.MONITOR_DEFAULTTONEAREST),
                    Bounds = FromNative(rect),
                    IsVisible = NativeMethods.IsWindowVisible(window),
                    IsPrimary = isPrimary
                });
            }
        }

        static Rect FromNative(NativeMethods.RECT rect)
        {
            return new Rect(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        static NativeMethods.RECT ToNative(Rect rect)
        {
            var native = new NativeMethods.RECT();
            native.Left = rect.Left;
            native.Top = rect.Top;
            native.Right = rect.Right;
            native.Bottom = rect.Bottom;
            return native;
        }
    }
}
