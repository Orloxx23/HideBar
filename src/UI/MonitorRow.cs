using System;
using System.Drawing;
using System.Windows.Forms;
using HideBar.Core;
using HideBar.UI.Controls;

namespace HideBar.UI
{
    // One display in the Monitors card: its number, name, details and the switch that hides its
    // taskbar. Clicking anywhere on the row flips the switch.
    class MonitorRow : Panel
    {
        readonly Theme theme;
        readonly NumberBadge badge;
        readonly Label nameLabel;
        readonly Label detailLabel;
        readonly Label noteLabel;
        readonly ToggleSwitch toggle;
        readonly int textLeft;
        bool updating;

        // Raised only when the user flips the switch.
        public event EventHandler PickedChanged;

        public MonitorRow(Theme theme, int width)
        {
            this.theme = theme;
            BackColor = theme.Surface;
            DoubleBuffered = true;
            Size = new Size(width, Style.S(56));

            badge = new NumberBadge(theme);
            badge.Location = new Point(0, (Height - badge.Height) / 2);

            toggle = new ToggleSwitch(theme);
            toggle.Location = new Point(Width - toggle.Width, (Height - toggle.Height) / 2);
            toggle.CheckedChanged += delegate { if (!updating) OnPickedChanged(); };

            textLeft = badge.Right + Style.S(12);
            nameLabel = MakeLine(Style.BodyStrong, theme.Text, Style.S(8));
            detailLabel = MakeLine(Style.Small, theme.SubtleText, nameLabel.Bottom + Style.S(1));
            noteLabel = Style.MakeLabel("", Style.Small, theme.SubtleText, theme.Surface);

            Controls.AddRange(new Control[] { badge, nameLabel, detailLabel, noteLabel, toggle });
            foreach (Control control in new Control[] { this, badge, nameLabel, detailLabel })
            {
                control.MouseClick += delegate { if (toggle.Visible) toggle.Checked = !toggle.Checked; };
            }
        }

        public bool Picked { get { return toggle.Checked; } }

        public void Bind(Display display, bool picked, string name, string details)
        {
            updating = true;

            badge.SetNumber(display.Number, picked);
            SetText(nameLabel, name);
            SetText(detailLabel, details);

            bool canHide = display.CanHideTaskbar;
            toggle.Visible = canHide;
            if (toggle.Checked != picked) toggle.Checked = picked;
            toggle.AccessibleName = "Hide the taskbar on " + name;

            // Displays without a switch get a short note in its place.
            noteLabel.Visible = !canHide;
            int textRight = toggle.Left;
            if (!canHide)
            {
                SetText(noteLabel, display.IsPrimary ? "Always keeps its taskbar" : "Can't be identified");
                noteLabel.Size = noteLabel.PreferredSize;
                noteLabel.Location = new Point(Width - noteLabel.Width, (Height - noteLabel.Height) / 2);
                textRight = noteLabel.Left;
            }
            nameLabel.Width = detailLabel.Width = textRight - Style.S(12) - textLeft;
            Cursor = canHide ? Cursors.Hand : Cursors.Default;

            updating = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(theme.Border)) e.Graphics.DrawLine(pen, 0, 0, Width, 0);
        }

        Label MakeLine(Font font, Color color, int top)
        {
            Label label = Style.MakeLabel("", font, color, theme.Surface);
            label.AutoSize = false;
            label.AutoEllipsis = true;
            label.SetBounds(textLeft, top, toggle.Left - Style.S(12) - textLeft, font.Height + Style.S(2));
            return label;
        }

        void OnPickedChanged()
        {
            EventHandler handler = PickedChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        static void SetText(Label label, string text)
        {
            if (label.Text != text) label.Text = text;
        }
    }
}
