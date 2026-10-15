// HideBar: hides the Windows taskbar on the monitors you pick and keeps it on the rest.
//
// Usage:
//   HideBar.exe            start it, or open its window if it is already running
//   HideBar.exe --exit     close the running instance and bring the taskbars back
//   HideBar.exe --startup  start without opening the window (used by "Start with Windows")

using System;
using System.Threading;
using System.Windows.Forms;
using HideBar.Platform;

namespace HideBar
{
    static class Program
    {
        public const string StartupArgument = "--startup";
        const string ExitArgument = "--exit";
        const string SingleInstanceName = "HideBar_SingleInstance";

        public static string Version
        {
            get { return typeof(Program).Assembly.GetName().Version.ToString(3); }
        }

        [STAThread]
        static void Main(string[] args)
        {
            string command = args.Length > 0 ? args[0].ToLowerInvariant() : "";
            if (command == ExitArgument)
            {
                InstanceSignals.SendExit();
                return;
            }

            bool isFirstInstance;
            using (new Mutex(true, SingleInstanceName, out isFirstInstance))
            {
                // Opening HideBar again brings up the running instance's window, which is the way
                // back in when the tray icon is turned off.
                if (!isFirstInstance)
                {
                    InstanceSignals.SendShow();
                    return;
                }

                NativeMethods.SetProcessDPIAware();
                UseCurrentAccessibility();
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new HideBarApp(command == StartupArgument));
            }
        }

        // Apps built for .NET 4.0 get WinForms' legacy accessibility, where screen readers see
        // every control as an unnamed pane. These switches turn on the current behavior: real
        // roles, names and toggle states. They must be set before any control is created.
        static void UseCurrentAccessibility()
        {
            AppContext.SetSwitch("Switch.UseLegacyAccessibilityFeatures", false);
            AppContext.SetSwitch("Switch.UseLegacyAccessibilityFeatures.2", false);
            AppContext.SetSwitch("Switch.UseLegacyAccessibilityFeatures.3", false);
            AppContext.SetSwitch("Switch.UseLegacyAccessibilityFeatures.4", false);
        }
    }
}
