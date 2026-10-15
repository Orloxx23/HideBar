using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HideBar.UI.Controls
{
    // The round number that ties a monitor row to its picture in the layout. It fills with the
    // accent color while HideBar hides that monitor's taskbar.
    class NumberBadge : Control
    {
        readonly Theme theme;
        int number;
        bool highlighted;

        public NumberBadge(Theme theme)
        {
            this.theme = theme;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            BackColor = theme.Surface;
            Size = new Size(Style.S(26), Style.S(26));
        }

        public void SetNumber(int value, bool isHighlighted)
        {
            if (value == number && isHighlighted == highlighted) return;
            number = value;
            highlighted = isHighlighted;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var circle = new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f);
            using (var fill = new SolidBrush(highlighted ? theme.Accent : theme.Background))
            {
                g.FillEllipse(fill, circle);
            }
            if (!highlighted)
            {
                using (var pen = new Pen(theme.Border)) g.DrawEllipse(pen, circle);
            }

            TextRenderer.DrawText(g, number.ToString(), Style.BodyStrong, ClientRectangle,
                highlighted ? theme.OnAccent : theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
        }
    }
}
