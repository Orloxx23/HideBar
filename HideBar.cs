// HideBar: hides the Windows taskbar on the monitors you pick and leaves it alone on the rest.
// It lives in the notification area; when it exits, the hidden taskbars come back.
//
// Usage:
//   HideBar.exe          start it (does nothing if it is already running)
//   HideBar.exe --exit   close the running instance and bring the taskbars back

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace HideBar
{
    static class Program
    {
        public const string ExitEventName = "HideBar_Exit";
        const string SingleInstanceName = "HideBar_SingleInstance";

        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("--exit", StringComparison.OrdinalIgnoreCase))
            {
                EventWaitHandle exitEvent;
                if (EventWaitHandle.TryOpenExisting(ExitEventName, out exitEvent))
                {
                    exitEvent.Set();
                    exitEvent.Dispose();
                }
                return;
            }

            bool isFirstInstance;
            using (new Mutex(true, SingleInstanceName, out isFirstInstance))
            {
                if (!isFirstInstance) return;

                Native.SetProcessDPIAware();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new TrayApp());
            }
        }
    }

    class TrayApp : ApplicationContext
    {
        const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunValueName = "HideBar";

        readonly NotifyIcon trayIcon;
        readonly ToolStripMenuItem monitorsItem;
        readonly ToolStripMenuItem pauseItem;
        readonly ToolStripMenuItem startupItem;
        readonly System.Windows.Forms.Timer timer;
        readonly EventWaitHandle exitEvent;
        readonly RegisteredWaitHandle exitWait;
        readonly HashSet<string> targetIds;
        readonly List<IntPtr> hiddenTaskbars = new List<IntPtr>();
        volatile bool exitRequested;
        bool paused;
        bool exiting;

        public TrayApp()
        {
            targetIds = Settings.LoadMonitorIds();

            monitorsItem = new ToolStripMenuItem("Hide the taskbar on");
            // A placeholder so the submenu arrow shows; the list is rebuilt every time it opens.
            monitorsItem.DropDownItems.Add("...");
            monitorsItem.DropDownOpening += delegate { FillMonitorsMenu(); };

            pauseItem = new ToolStripMenuItem("Pause");
            pauseItem.Click += delegate { SetPaused(!paused); };

            startupItem = new ToolStripMenuItem("Start with Windows");
            startupItem.Checked = IsStartupEnabled();
            startupItem.Click += delegate { SetStartup(!startupItem.Checked); };

            var menu = new ContextMenuStrip();
            menu.Items.Add(monitorsItem);
            menu.Items.Add(pauseItem);
            menu.Items.Add(startupItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit and show the taskbar again", null, delegate { Exit(); });

            trayIcon = new NotifyIcon();
            trayIcon.Icon = CreateIcon();
            trayIcon.ContextMenuStrip = menu;
            trayIcon.Text = "HideBar";
            trayIcon.DoubleClick += delegate { SetPaused(!paused); };
            trayIcon.Visible = true;

            // Explorer shows the taskbar again when it restarts, when a monitor is reconnected
            // or when the display layout changes, so the state is checked every second as well
            // as on display changes.
            timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000;
            timer.Tick += delegate
            {
                if (exitRequested) Exit();
                else Apply();
            };
            timer.Start();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            // "HideBar.exe --exit" signals this event from another process.
            exitEvent = new EventWaitHandle(false, EventResetMode.AutoReset, Program.ExitEventName);
            exitWait = ThreadPool.RegisterWaitForSingleObject(exitEvent, delegate { exitRequested = true; }, null, -1, true);

            Apply();
            if (targetIds.Count == 0)
            {
                trayIcon.ShowBalloonTip(5000, "HideBar",
                    "Right-click this icon and pick the monitor where the taskbar should be hidden.",
                    ToolTipIcon.Info);
            }
        }

        void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            Apply();
        }

        void Apply()
        {
            if (paused || targetIds.Count == 0)
            {
                UpdateTooltip(0);
                return;
            }

            using (new Native.DpiScope())
            {
                hiddenTaskbars.RemoveAll(taskbar => !Native.Exists(taskbar));
                List<IntPtr> taskbars = Native.GetSecondaryTaskbars();
                int hiddenOn = 0;

                foreach (Display display in Native.GetDisplays())
                {
                    if (!IsTarget(display)) continue;

                    foreach (IntPtr taskbar in taskbars)
                    {
                        if (Native.MonitorOf(taskbar) != display.Handle) continue;
                        if (Native.IsVisible(taskbar)) Native.Hide(taskbar);
                        if (!hiddenTaskbars.Contains(taskbar)) hiddenTaskbars.Add(taskbar);
                    }

                    // Windows keeps reserving the hidden taskbar's space, so maximized windows
                    // would leave a gap. The space is given back without broadcasting the
                    // change, because the broadcast makes Explorer show the taskbar again.
                    if (!display.WorkArea.Equals(display.Bounds)) Native.SetWorkArea(display.Bounds, false);
                    hiddenOn++;
                }

                UpdateTooltip(hiddenOn);
            }
        }

        // Shows the taskbars this app hid and gives their space back.
        void Restore()
        {
            using (new Native.DpiScope())
            {
                List<IntPtr> taskbars = Native.GetSecondaryTaskbars();
                foreach (IntPtr taskbar in taskbars)
                {
                    if (hiddenTaskbars.Contains(taskbar) && !Native.IsVisible(taskbar)) Native.Show(taskbar);
                }
                hiddenTaskbars.Clear();

                bool autoHide = Native.IsTaskbarAutoHide();
                foreach (Display display in Native.GetDisplays())
                {
                    if (!IsTarget(display)) continue;

                    Native.RECT workArea = display.Bounds;
                    if (!autoHide)
                    {
                        foreach (IntPtr taskbar in taskbars)
                        {
                            if (Native.MonitorOf(taskbar) == display.Handle)
                                workArea = Native.SubtractTaskbar(workArea, Native.RectOf(taskbar));
                        }
                    }
                    if (!workArea.Equals(display.WorkArea)) Native.SetWorkArea(workArea, true);
                }
            }
        }

        // The main display's taskbar is a different window that Explorer depends on, so only
        // secondary displays can be picked.
        bool IsTarget(Display display)
        {
            return !display.IsPrimary && display.ModelId != null && targetIds.Contains(display.ModelId);
        }

        void FillMonitorsMenu()
        {
            monitorsItem.DropDownItems.Clear();
            List<Display> displays;
            using (new Native.DpiScope())
            {
                displays = Native.GetDisplays();
            }

            foreach (Display display in displays)
            {
                string modelId = display.ModelId;
                var item = new ToolStripMenuItem(Describe(display));
                item.Checked = modelId != null && targetIds.Contains(modelId);
                item.Enabled = modelId != null && !display.IsPrimary;
                item.Click += delegate { ToggleMonitor(modelId); };
                monitorsItem.DropDownItems.Add(item);
            }
        }

        static string Describe(Display display)
        {
            string text = string.Format("{0} x {1}   {2}", display.Bounds.Width, display.Bounds.Height,
                display.Name ?? "Unknown monitor");
            if (display.ModelId != null) text += "   [" + display.ModelId + "]";
            if (display.IsPrimary) text += "   (main display, not supported)";
            return text;
        }

        void ToggleMonitor(string modelId)
        {
            // Bring every hidden taskbar back first, then hide again with the new selection.
            Restore();
            if (!targetIds.Remove(modelId)) targetIds.Add(modelId);
            Settings.SaveMonitorIds(targetIds);
            Apply();
        }

        void SetPaused(bool value)
        {
            paused = value;
            pauseItem.Checked = value;
            if (paused) Restore();
            Apply();
        }

        void UpdateTooltip(int hiddenOn)
        {
            string text;
            if (paused) text = "HideBar: paused";
            else if (targetIds.Count == 0) text = "HideBar: no monitor selected";
            else if (hiddenOn == 0) text = "HideBar: selected monitor not connected";
            else if (hiddenOn == 1) text = "HideBar: taskbar hidden on 1 monitor";
            else text = "HideBar: taskbar hidden on " + hiddenOn + " monitors";

            if (trayIcon.Text != text) trayIcon.Text = text;
        }

        static bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath))
            {
                return key != null && key.GetValue(RunValueName) != null;
            }
        }

        void SetStartup(bool enable)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true))
            {
                if (key == null) return;
                if (enable) key.SetValue(RunValueName, "\"" + Application.ExecutablePath + "\"");
                else key.DeleteValue(RunValueName, false);
            }
            startupItem.Checked = IsStartupEnabled();
        }

        void Exit()
        {
            if (exiting) return;
            exiting = true;

            timer.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            Restore();
            exitWait.Unregister(null);
            exitEvent.Dispose();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            ExitThread();
        }

        static Icon CreateIcon()
        {
            using (var bitmap = new Bitmap(16, 16))
            {
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.Clear(Color.Transparent);
                    g.FillRectangle(Brushes.White, 1, 1, 14, 11);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(32, 32, 32)), 2, 2, 12, 9);
                    g.FillRectangle(new SolidBrush(Color.FromArgb(0, 170, 255)), 3, 3, 10, 5);
                    g.FillRectangle(Brushes.White, 7, 12, 2, 2);
                    g.FillRectangle(Brushes.White, 4, 14, 8, 1);
                }
                return Icon.FromHandle(bitmap.GetHicon());
            }
        }
    }

    class Display
    {
        public IntPtr Handle;
        public string DeviceName;   // e.g. \\.\DISPLAY3
        public string ModelId;      // e.g. DWE2100, read from the monitor's EDID
        public string Name;         // e.g. Generic Monitor (ASUS VP249)
        public bool IsPrimary;
        public Native.RECT Bounds;
        public Native.RECT WorkArea;
    }

    // The monitors to hide the taskbar on, one model ID per line in %APPDATA%\HideBar\monitors.txt.
    static class Settings
    {
        static readonly string FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HideBar", "monitors.txt");

        public static HashSet<string> LoadMonitorIds()
        {
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(FilePath)) return ids;

            foreach (string line in File.ReadAllLines(FilePath))
            {
                string id = line.Trim();
                if (id.Length > 0 && !id.StartsWith("#")) ids.Add(id);
            }
            return ids;
        }

        public static void SaveMonitorIds(IEnumerable<string> ids)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            File.WriteAllLines(FilePath, ids);
        }
    }

    static class Native
    {
        const int SW_HIDE = 0;
        const int SW_SHOWNA = 8;
        const uint MONITORINFOF_PRIMARY = 1;
        const uint MONITOR_DEFAULTTONEAREST = 2;
        const uint EDD_GET_DEVICE_INTERFACE_NAME = 1;
        const int DISPLAY_DEVICE_ACTIVE = 1;
        const uint SPI_SETWORKAREA = 0x002F;
        const uint SPIF_SENDCHANGE = 0x02;
        const uint ABM_GETSTATE = 4;
        const uint ABS_AUTOHIDE = 1;
        static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;

            public int Width { get { return Right - Left; } }
            public int Height { get { return Bottom - Top; } }

            public override bool Equals(object obj)
            {
                if (!(obj is RECT)) return false;
                RECT other = (RECT)obj;
                return Left == other.Left && Top == other.Top && Right == other.Right && Bottom == other.Bottom;
            }

            public override int GetHashCode()
            {
                return Left ^ (Top << 8) ^ (Right << 16) ^ (Bottom << 24);
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public uint uCallbackMessage;
            public uint uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data);

        [DllImport("user32.dll")] static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc proc, IntPtr data);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX info);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern bool EnumDisplayDevices(string device, uint index, ref DISPLAY_DEVICE displayDevice, uint flags);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindWindowEx(IntPtr parent, IntPtr after, string className, string title);
        [DllImport("user32.dll")] static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hwnd, int command);
        [DllImport("user32.dll")] static extern bool SystemParametersInfo(uint action, uint param, ref RECT rect, uint winIni);
        [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("shell32.dll")] static extern UIntPtr SHAppBarMessage(uint message, ref APPBARDATA data);

        // The process is system DPI aware so the tray menu renders crisply, but monitor and
        // taskbar coordinates are read and written per monitor: with mixed scaling, Windows
        // would otherwise hand out virtualized coordinates and the work area would be wrong.
        public sealed class DpiScope : IDisposable
        {
            readonly IntPtr previous;

            public DpiScope()
            {
                try
                {
                    previous = SetThreadDpiAwarenessContext(PerMonitorAwareV2);
                }
                catch (EntryPointNotFoundException)
                {
                }
            }

            public void Dispose()
            {
                if (previous != IntPtr.Zero) SetThreadDpiAwarenessContext(previous);
            }
        }

        public static List<Display> GetDisplays()
        {
            var displays = new List<Display>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, delegate (IntPtr hMonitor, IntPtr hdc, ref RECT rect, IntPtr data)
            {
                var info = new MONITORINFOEX();
                info.cbSize = Marshal.SizeOf(typeof(MONITORINFOEX));
                if (!GetMonitorInfo(hMonitor, ref info)) return true;

                var display = new Display();
                display.Handle = hMonitor;
                display.DeviceName = info.szDevice;
                display.Bounds = info.rcMonitor;
                display.WorkArea = info.rcWork;
                display.IsPrimary = (info.dwFlags & MONITORINFOF_PRIMARY) != 0;
                ReadMonitorDevice(display);
                displays.Add(display);
                return true;
            }, IntPtr.Zero);
            return displays;
        }

        // The monitor's device path looks like \\?\DISPLAY#DWE2100#5&2521ec4&0&UID4356#{...}.
        // Its second part is the model ID from the EDID, which stays the same when the monitor
        // is plugged into another port.
        static void ReadMonitorDevice(Display display)
        {
            var device = new DISPLAY_DEVICE();
            device.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            for (uint i = 0; EnumDisplayDevices(display.DeviceName, i, ref device, EDD_GET_DEVICE_INTERFACE_NAME); i++)
            {
                string[] parts = (device.DeviceID ?? "").Split('#');
                if (parts.Length > 1)
                {
                    bool isActive = (device.StateFlags & DISPLAY_DEVICE_ACTIVE) != 0;
                    if (display.ModelId == null || isActive)
                    {
                        display.ModelId = parts[1];
                        display.Name = device.DeviceString;
                    }
                    if (isActive) return;
                }
                device.cb = Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            }
        }

        public static List<IntPtr> GetSecondaryTaskbars()
        {
            var taskbars = new List<IntPtr>();
            IntPtr window = IntPtr.Zero;
            while ((window = FindWindowEx(IntPtr.Zero, window, "Shell_SecondaryTrayWnd", null)) != IntPtr.Zero)
                taskbars.Add(window);
            return taskbars;
        }

        public static IntPtr MonitorOf(IntPtr window)
        {
            return MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
        }

        public static RECT RectOf(IntPtr window)
        {
            RECT rect;
            GetWindowRect(window, out rect);
            return rect;
        }

        public static bool Exists(IntPtr window)
        {
            return IsWindow(window);
        }

        public static bool IsVisible(IntPtr window)
        {
            return IsWindowVisible(window);
        }

        public static void Hide(IntPtr window)
        {
            ShowWindow(window, SW_HIDE);
        }

        public static void Show(IntPtr window)
        {
            ShowWindow(window, SW_SHOWNA);
        }

        public static bool IsTaskbarAutoHide()
        {
            var data = new APPBARDATA();
            data.cbSize = Marshal.SizeOf(typeof(APPBARDATA));
            return ((ulong)SHAppBarMessage(ABM_GETSTATE, ref data) & ABS_AUTOHIDE) != 0;
        }

        // Removes the screen edge the taskbar sits on from the work area.
        public static RECT SubtractTaskbar(RECT workArea, RECT taskbar)
        {
            if (taskbar.Width >= taskbar.Height)
            {
                if (taskbar.Bottom >= workArea.Bottom && taskbar.Top > workArea.Top) workArea.Bottom = taskbar.Top;
                else if (taskbar.Top <= workArea.Top) workArea.Top = taskbar.Bottom;
            }
            else
            {
                if (taskbar.Left <= workArea.Left) workArea.Left = taskbar.Right;
                else if (taskbar.Right >= workArea.Right) workArea.Right = taskbar.Left;
            }
            return workArea;
        }

        public static void SetWorkArea(RECT area, bool broadcast)
        {
            SystemParametersInfo(SPI_SETWORKAREA, 0, ref area, broadcast ? SPIF_SENDCHANGE : 0);
        }
    }
}
