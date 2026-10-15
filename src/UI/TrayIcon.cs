using System;
using System.Drawing;
using System.Windows.Forms;
using HideBar.Core;

namespace HideBar.UI
{
    // The notification-area icon and its menu. A left click opens the window.
    sealed class TrayIcon : IDisposable
    {
        const int MaxToolTipLength = 63;

        readonly TaskbarController controller;
        readonly NotifyIcon notifyIcon;
        readonly ToolStripMenuItem pauseItem;

        public TrayIcon(TaskbarController controller, Action openWindow, Action exit)
        {
            this.controller = controller;

            var openItem = new ToolStripMenuItem("Open HideBar");
            openItem.Font = new Font(openItem.Font, FontStyle.Bold);
            openItem.Click += delegate { openWindow(); };

            pauseItem = new ToolStripMenuItem("Pause");
            pauseItem.Click += delegate { controller.SetPaused(!controller.IsPaused); };

            var menu = new ContextMenuStrip();
            menu.Items.Add(openItem);
            menu.Items.Add(pauseItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit and show the taskbar again", null, delegate { exit(); });

            notifyIcon = new NotifyIcon { Icon = AppIcon.Small, ContextMenuStrip = menu, Text = "HideBar" };
            notifyIcon.MouseClick += delegate (object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) openWindow();
            };

            controller.StateChanged += OnStateChanged;
            RefreshState();
        }

        public void Dispose()
        {
            controller.StateChanged -= OnStateChanged;
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }

        void OnStateChanged(object sender, EventArgs e)
        {
            RefreshState();
        }

        void RefreshState()
        {
            notifyIcon.Visible = controller.ShowTrayIcon;
            pauseItem.Checked = controller.IsPaused;

            string text = "HideBar · " + Strings.Status(controller);
            if (text.Length > MaxToolTipLength) text = text.Substring(0, MaxToolTipLength);
            if (notifyIcon.Text != text) notifyIcon.Text = text;
        }
    }
}
