using System;
using System.Drawing;
using System.Windows.Forms;
using HideBar.UI.Controls;

namespace HideBar.UI
{
    // A setting in the Options card: a title, a short explanation and a switch. Clicking
    // anywhere on the row flips the switch.
    class OptionRow : Panel
    {
        readonly Theme theme;
        readonly ToggleSwitch toggle;
        bool updating;

        // Raised only when the user flips the switch.
        public event EventHandler CheckedChanged;

        public OptionRow(Theme theme, int width, string title, string description)
        {
            this.theme = theme;
            BackColor = theme.Surface;
            DoubleBuffered = true;
            Width = width;
            Cursor = Cursors.Hand;

            toggle = new ToggleSwitch(theme);
            toggle.AccessibleName = title;
            toggle.CheckedChanged += delegate { if (!updating) OnCheckedChanged(); };

            int textWidth = width - toggle.Width - Style.S(16);
            Label titleLabel = Style.MakeLabel(title, Style.Body, theme.Text, theme.Surface);
            titleLabel.Location = new Point(0, Style.S(9));
            Label descriptionLabel = Style.MakeParagraph(description, Style.Small, theme.SubtleText, theme.Surface, textWidth);
            descriptionLabel.Location = new Point(0, titleLabel.Bottom + Style.S(1));

            Height = Math.Max(Style.S(56), descriptionLabel.Bottom + Style.S(9));
            toggle.Location = new Point(width - toggle.Width, (Height - toggle.Height) / 2);

            Controls.AddRange(new Control[] { titleLabel, descriptionLabel, toggle });
            foreach (Control control in new Control[] { this, titleLabel, descriptionLabel })
            {
                control.MouseClick += delegate { toggle.Checked = !toggle.Checked; };
            }
        }

        public bool Checked
        {
            get { return toggle.Checked; }
            set
            {
                if (toggle.Checked == value) return;
                updating = true;
                toggle.Checked = value;
                updating = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(theme.Border)) e.Graphics.DrawLine(pen, 0, 0, Width, 0);
        }

        void OnCheckedChanged()
        {
            EventHandler handler = CheckedChanged;
            if (handler != null) handler(this, EventArgs.Empty);
        }
    }
}
