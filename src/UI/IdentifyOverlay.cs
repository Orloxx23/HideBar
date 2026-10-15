using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using HideBar.Core;
using HideBar.Platform;

namespace HideBar.UI
{
    // A big number shown for a moment in the middle of each display, to tell them apart.
    class IdentifyOverlay : Form
    {
        const int VisibleMilliseconds = 2500;

        readonly Theme theme;
        readonly int number;
        readonly Timer closeTimer;

        public static void ShowFor(IEnumerable<Display> displays, Theme theme)
        {
            foreach (Display display in displays)
            {
                Screen screen = FindScreen(display.DeviceName);
                if (screen != null) new IdentifyOverlay(display.Number, screen.Bounds, theme).Show();
            }
        }

        IdentifyOverlay(int number, Rectangle screenBounds, Theme theme)
        {
            this.number = number;
            this.theme = theme;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = theme.Accent;
            DoubleBuffered = true;

            int size = Style.S(140);
            Bounds = new Rectangle(
                screenBounds.Left + (screenBounds.Width - size) / 2,
                screenBounds.Top + (screenBounds.Height - size) / 2,
                size, size);

            closeTimer = new Timer { Interval = VisibleMilliseconds };
            closeTimer.Tick += delegate { Close(); };
            closeTimer.Start();
            Click += delegate { Close(); };
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_TOOLWINDOW = 0x00000080;
                const int WS_EX_NOACTIVATE = 0x08000000;
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE;
                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            WindowStyling.UseRoundedCorners(Handle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            using (var font = new Font("Segoe UI Semibold", Height * 0.5f, GraphicsUnit.Pixel))
            {
                TextRenderer.DrawText(e.Graphics, number.ToString(), font, ClientRectangle, theme.OnAccent,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) closeTimer.Dispose();
            base.Dispose(disposing);
        }

        static Screen FindScreen(string deviceName)
        {
            foreach (Screen screen in Screen.AllScreens)
            {
                if (screen.DeviceName == deviceName) return screen;
            }
            return null;
        }
    }
}
