using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HideBar.UI.Controls
{
    // A rounded panel on the window background, like the cards in Windows 11 Settings.
    class Card : Panel
    {
        protected readonly Theme theme;

        public Card(Theme theme)
        {
            this.theme = theme;
            BackColor = theme.Surface;
            DoubleBuffered = true;
            SetStyle(ControlStyles.ResizeRedraw, true);
        }

        // Space between the card's edge and its content.
        public static int Inset { get { return Style.S(16); } }

        protected int InnerWidth { get { return Width - 2 * Inset; } }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(theme.Background);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new RectangleF(0.5f, 0.5f, Width - 1.5f, Height - 1.5f);
            using (GraphicsPath path = Style.RoundedRect(bounds, Style.S(8f)))
            using (var fill = new SolidBrush(theme.Surface))
            using (var border = new Pen(theme.Border))
            {
                g.FillPath(fill, path);
                g.DrawPath(border, path);
            }
        }
    }
}
