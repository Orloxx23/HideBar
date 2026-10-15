using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using HideBar.Core;

namespace HideBar.UI.Controls
{
    // Draws the displays where they sit on the desk, like Windows' display settings. Clicking a
    // display, or pressing Space on it after moving with the arrow keys, raises DisplayClicked.
    class MonitorLayoutView : Control
    {
        readonly Theme theme;
        readonly ToolTip toolTip = new ToolTip();
        List<Display> displays = new List<Display>();
        RectangleF[] tiles = new RectangleF[0];
        Func<Display, bool> isPicked = delegate { return false; };
        Func<Display, string> describe = delegate { return ""; };
        int hoverIndex = -1;
        int focusIndex;
        string toolTipText;

        public event Action<Display> DisplayClicked;

        public MonitorLayoutView(Theme theme)
        {
            this.theme = theme;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.Selectable, true);
            TabStop = true;
            BackColor = theme.Surface;
            AccessibleName = "Monitor layout";
            AccessibleRole = AccessibleRole.Diagram;
        }

        public void SetDisplays(List<Display> list, Func<Display, bool> picked, Func<Display, string> description)
        {
            displays = list;
            isPicked = picked;
            describe = description;
            if (focusIndex >= displays.Count) focusIndex = 0;
            ComputeTiles();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) toolTip.Dispose();
            base.Dispose(disposing);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            ComputeTiles();
        }

        // Scales the virtual desktop to fit, keeping its proportions.
        void ComputeTiles()
        {
            tiles = new RectangleF[displays.Count];
            if (displays.Count == 0) return;

            int left = int.MaxValue, top = int.MaxValue, right = int.MinValue, bottom = int.MinValue;
            foreach (Display display in displays)
            {
                left = Math.Min(left, display.Bounds.Left);
                top = Math.Min(top, display.Bounds.Top);
                right = Math.Max(right, display.Bounds.Right);
                bottom = Math.Max(bottom, display.Bounds.Bottom);
            }

            float padding = Style.S(12f);
            float availableWidth = Width - 2 * padding;
            float availableHeight = Height - 2 * padding;
            float scale = Math.Min(availableWidth / (right - left), availableHeight / (bottom - top));
            float offsetX = padding + (availableWidth - (right - left) * scale) / 2;
            float offsetY = padding + (availableHeight - (bottom - top) * scale) / 2;
            float gap = Style.S(3f);

            for (int i = 0; i < displays.Count; i++)
            {
                Rect bounds = displays[i].Bounds;
                var tile = new RectangleF(
                    offsetX + (bounds.Left - left) * scale,
                    offsetY + (bounds.Top - top) * scale,
                    bounds.Width * scale,
                    bounds.Height * scale);
                tile.Inflate(-gap, -gap);
                tiles[i] = tile;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            for (int i = 0; i < tiles.Length; i++) DrawTile(g, displays[i], tiles[i], i);
        }

        void DrawTile(Graphics g, Display display, RectangleF tile, int index)
        {
            bool picked = isPicked(display);
            bool hovered = index == hoverIndex && display.CanHideTaskbar;
            float radius = Math.Min(Style.S(6f), tile.Height / 4);

            Color fill = picked ? Style.Mix(theme.Accent, theme.Surface, 0.2f) : theme.Background;
            Color border = picked ? theme.Accent : hovered ? theme.SubtleText : theme.Border;
            using (GraphicsPath path = Style.RoundedRect(tile, radius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border, picked ? Style.S(2f) : 1f))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            // The taskbar along the bottom edge: solid while it shows, dashed while HideBar hides it.
            float barHeight = Math.Max(Style.S(4f), tile.Height * 0.07f);
            float barInset = Style.S(4f);
            var bar = new RectangleF(tile.Left + barInset, tile.Bottom - barHeight - barInset, tile.Width - 2 * barInset, barHeight);
            if (display.TaskbarVisible)
            {
                using (var brush = new SolidBrush(Color.FromArgb(150, theme.SubtleText))) g.FillRectangle(brush, bar);
            }
            else if (picked)
            {
                using (var pen = new Pen(theme.Accent) { DashStyle = DashStyle.Dash }) g.DrawRectangle(pen, bar.X, bar.Y, bar.Width, bar.Height);
            }

            var textArea = new RectangleF(tile.Left, tile.Top, tile.Width, bar.Top - tile.Top);
            string caption = display.IsPrimary ? "Main" : picked ? "Hidden" : null;
            DrawLabel(g, display.Number.ToString(), caption, textArea);

            if (Focused && ShowFocusCues && index == focusIndex)
            {
                RectangleF ring = RectangleF.Inflate(tile, -Style.S(4f), -Style.S(4f));
                using (GraphicsPath path = Style.RoundedRect(ring, radius / 2))
                using (var pen = new Pen(theme.Text) { DashStyle = DashStyle.Dot })
                {
                    g.DrawPath(pen, path);
                }
            }
        }

        // The display's number in the middle, with a caption under it when the tile has room.
        void DrawLabel(Graphics g, string number, string caption, RectangleF area)
        {
            float numberSize = Math.Max(Style.S(11f), Math.Min(Style.S(26f), area.Height * 0.4f));
            using (var numberFont = new Font("Segoe UI Semibold", numberSize, GraphicsUnit.Pixel))
            using (var textBrush = new SolidBrush(theme.Text))
            using (var subtleBrush = new SolidBrush(theme.SubtleText))
            using (var centered = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near })
            {
                float numberHeight = numberFont.GetHeight(g);
                float captionHeight = Style.Small.GetHeight(g);
                bool showCaption = caption != null
                    && area.Height >= numberHeight + captionHeight + Style.S(8f)
                    && g.MeasureString(caption, Style.Small).Width <= area.Width - Style.S(6f);

                float blockHeight = numberHeight + (showCaption ? captionHeight : 0);
                float top = area.Top + (area.Height - blockHeight) / 2;
                g.DrawString(number, numberFont, textBrush, new RectangleF(area.Left, top, area.Width, numberHeight), centered);
                if (showCaption)
                {
                    g.DrawString(caption, Style.Small, subtleBrush,
                        new RectangleF(area.Left, top + numberHeight, area.Width, captionHeight), centered);
                }
            }
        }

        int HitTest(Point point)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (tiles[i].Contains(point)) return i;
            }
            return -1;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = HitTest(e.Location);
            if (index == hoverIndex) return;

            hoverIndex = index;
            Cursor = index >= 0 && displays[index].CanHideTaskbar ? Cursors.Hand : Cursors.Default;
            string text = index >= 0 ? describe(displays[index]) : null;
            if (text != toolTipText)
            {
                toolTipText = text;
                toolTip.SetToolTip(this, text);
            }
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            int index = HitTest(e.Location);
            if (e.Button != MouseButtons.Left || index < 0) return;
            focusIndex = index;
            RaiseDisplayClicked(displays[index]);
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData)
            {
                case Keys.Left:
                case Keys.Right:
                case Keys.Up:
                case Keys.Down:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (displays.Count == 0) return;

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Up:
                    focusIndex = (focusIndex + displays.Count - 1) % displays.Count;
                    break;
                case Keys.Right:
                case Keys.Down:
                    focusIndex = (focusIndex + 1) % displays.Count;
                    break;
                case Keys.Space:
                case Keys.Enter:
                    RaiseDisplayClicked(displays[focusIndex]);
                    break;
                default:
                    return;
            }
            e.Handled = true;
            Invalidate();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            Invalidate();
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            Invalidate();
        }

        void RaiseDisplayClicked(Display display)
        {
            if (!display.CanHideTaskbar) return;
            Action<Display> handler = DisplayClicked;
            if (handler != null) handler(display);
        }
    }
}
