using System;
using System.Runtime.InteropServices;

namespace HideBar.Platform
{
    // Win32 declarations. Only the classes in Platform use them.
    static class NativeMethods
    {
        public const int SW_HIDE = 0;
        public const int SW_SHOWNA = 8;
        public const uint MONITORINFOF_PRIMARY = 1;
        public const uint MONITOR_DEFAULTTONEAREST = 2;
        public const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
        public const int DISPLAY_DEVICE_ACTIVE = 1;
        public const uint SPI_SETWORKAREA = 0x002F;
        public const uint SPIF_SENDCHANGE = 0x02;
        public const uint ABM_GETSTATE = 4;
        public const uint ABS_AUTOHIDE = 1;
        public const int ASFW_ANY = -1;
        public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        public const int DWMWCP_ROUND = 2;
        public static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        public const uint QDC_ONLY_ACTIVE_PATHS = 2;
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME = 1;
        public const uint DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME = 2;

        [StructLayout(LayoutKind.Sequential)]
        public struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_SOURCE_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_TARGET_INFO
        {
            public LUID adapterId;
            public uint id;
            public uint modeInfoIdx;
            public uint outputTechnology;
            public uint rotation;
            public uint scaling;
            public uint refreshRateNumerator;
            public uint refreshRateDenominator;
            public uint scanLineOrdering;
            public int targetAvailable;
            public uint statusFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_PATH_INFO
        {
            public DISPLAYCONFIG_PATH_SOURCE_INFO sourceInfo;
            public DISPLAYCONFIG_PATH_TARGET_INFO targetInfo;
            public uint flags;
        }

        // HideBar never reads the modes, but QueryDisplayConfig needs room for them.
        [StructLayout(LayoutKind.Sequential, Size = 64)]
        public struct DISPLAYCONFIG_MODE_INFO
        {
            public uint infoType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DISPLAYCONFIG_DEVICE_INFO_HEADER
        {
            public uint type;
            public uint size;
            public LUID adapterId;
            public uint id;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string viewGdiDeviceName;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            public DISPLAYCONFIG_DEVICE_INFO_HEADER header;
            public uint flags;
            public uint outputTechnology;
            public ushort edidManufactureId;
            public ushort edidProductCodeId;
            public uint connectorInstance;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string monitorFriendlyDeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string monitorDevicePath;
        }

        [DllImport("user32.dll")]
        public static extern int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

        [DllImport("user32.dll")]
        public static extern int QueryDisplayConfig(uint flags, ref uint pathCount, [Out] DISPLAYCONFIG_PATH_INFO[] paths,
            ref uint modeCount, [Out] DISPLAYCONFIG_MODE_INFO[] modes, IntPtr currentTopologyId);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_SOURCE_DEVICE_NAME request);

        [DllImport("user32.dll")]
        public static extern int DisplayConfigGetDeviceInfo(ref DISPLAYCONFIG_TARGET_DEVICE_NAME request);

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayDevices(string device, uint index, ref DISPLAY_DEVICE displayDevice, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll")]
        public static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll")]
        public static extern bool SystemParametersInfo(uint action, uint param, ref RECT rect, uint winIni);

        [DllImport("user32.dll")]
        public static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);

        [DllImport("user32.dll")]
        public static extern bool SetProcessDPIAware();

        [DllImport("user32.dll")]
        public static extern bool AllowSetForegroundWindow(int processId);

        [DllImport("shell32.dll")]
        public static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);

        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
    }
}
