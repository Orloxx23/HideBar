using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HideBar.UI.Controls
{
    // An on/off switch drawn like the Windows 11 toggle. It is still a CheckBox underneath, so
    // focus, the Space key and screen readers work as usual.
    class ToggleSwitch : CheckBox
    {
        readonly Theme theme;
        bool hovered;

        public ToggleSwitch(Theme theme)
        {
            this.theme = theme;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            AutoSize = false;
            Size = new Size(Style.S(44), Style.S(24));
            BackColor = theme.Surface;
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnCheckedChanged(EventArgs e)
        {
            Invalidate();
            base.OnCheckedChanged(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            float inset = Style.S(3f);
            var track = new RectangleF(inset, inset, Width - 2 * inset - 1, Height - 2 * inset - 1);
            using (GraphicsPath path = Style.RoundedRect(track, track.Height / 2))
            {
                if (Checked)
                {
                    Color fill = hovered ? Style.Mix(theme.Accent, theme.Text, 0.88f) : theme.Accent;
                    using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                }
                else
                {
                    if (hovered)
                    {
                        using (var brush = new SolidBrush(theme.SurfaceHover)) g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(theme.Outline)) g.DrawPath(pen, path);
                }
            }

            // The knob grows a little on hover, as in Windows 11.
            float knob = track.Height - Style.S(hovered ? 7f : 9f);
            float x = Checked ? track.Right - Style.S(4f) - knob : track.Left + Style.S(4f);
            float y = track.Top + (track.Height - knob) / 2;
            using (var brush = new SolidBrush(Checked ? theme.OnAccent : theme.Outline))
            {
                g.FillEllipse(brush, x, y, knob, knob);
            }

            if (Focused && ShowFocusCues)
            {
                var ring = new RectangleF(0.5f, 0.5f, Width - 2f, Height - 2f);
                using (GraphicsPath path = Style.RoundedRect(ring, ring.Height / 2))
                using (var pen = new Pen(theme.Text))
                {
                    g.DrawPath(pen, path);
                }
            }
        }
    }
}
