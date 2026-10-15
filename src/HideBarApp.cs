using System;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
using HideBar.Core;
using HideBar.Platform;
using HideBar.UI;

namespace HideBar
{
    // Builds the app and keeps it alive until the user exits. This is the only place that knows
    // which concrete classes make up HideBar.
    class HideBarApp : ApplicationContext
    {
        const int CheckIntervalMilliseconds = 1000;

        readonly TaskbarController controller;
        readonly TrayIcon trayIcon;
        readonly InstanceSignals signals;
        readonly System.Windows.Forms.Timer timer;
        SettingsWindow window;
        bool exiting;

        public HideBarApp(bool launchedAtStartup)
        {
            var startup = new RunKeyStartup(Application.ExecutablePath, Program.StartupArgument);
            startup.Refresh();
            controller = new TaskbarController(
                new WindowsDisplaySystem(),
                new SettingsStore(SettingsStore.DefaultFolder),
                startup);

            trayIcon = new TrayIcon(controller, ShowWindow, Exit);

            // Signals arrive on a thread-pool thread; the UI work runs on this one.
            SynchronizationContext ui = new WindowsFormsSynchronizationContext();
            signals = new InstanceSignals(
                delegate { ui.Post(delegate { Exit(); }, null); },
                delegate { ui.Post(delegate { ShowWindow(); }, null); });

            timer = new System.Windows.Forms.Timer { Interval = CheckIntervalMilliseconds };
            timer.Tick += delegate { controller.Apply(); };
            timer.Start();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;

            controller.Apply();

            // Starting with Windows stays quiet; opening HideBar by hand shows the window.
            if (!launchedAtStartup || !controller.HasPickedDisplays) ShowWindow();
        }

        void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            controller.Apply();
        }

        void ShowWindow()
        {
            if (exiting) return;
            if (window == null)
            {
                window = new SettingsWindow(controller, Exit);
                window.FormClosed += delegate { window = null; };
                window.Show();
            }
            else if (window.WindowState == FormWindowState.Minimized)
            {
                window.WindowState = FormWindowState.Normal;
            }
            window.Activate();
        }

        void Exit()
        {
            if (exiting) return;
            exiting = true;

            timer.Stop();
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
            signals.Dispose();
            if (window != null) window.Close();
            controller.Restore();
            trayIcon.Dispose();
            ExitThread();
        }
    }
}
