using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace HideBar.UI
{
    // Fonts, DPI scaling and drawing helpers shared by the window and its controls.
    static class Style
    {
        static readonly float scale = ReadScale();

        public static readonly Font Title = new Font("Segoe UI Semibold", 17f);
        public static readonly Font Heading = new Font("Segoe UI Semibold", 11f);
        public static readonly Font Body = new Font("Segoe UI", 9.75f);
        public static readonly Font BodyStrong = new Font("Segoe UI Semibold", 9.75f);
        public static readonly Font Small = new Font("Segoe UI", 8.75f);

        // Sizes are written in 96 DPI pixels and scaled to the system DPI.
        public static int S(int pixels)
        {
            return (int)Math.Round(pixels * scale);
        }

        public static float S(float pixels)
        {
            return pixels * scale;
        }

        public static Label MakeLabel(string text, Font font, Color color, Color background)
        {
            var label = new Label
            {
                AutoSize = true,
                Font = font,
                ForeColor = color,
                BackColor = background,
                UseMnemonic = false,
                Text = text
            };
            label.Size = label.PreferredSize;
            return label;
        }

        // A label that wraps inside the given width.
        public static Label MakeParagraph(string text, Font font, Color color, Color background, int width)
        {
            Label label = MakeLabel(text, font, color, background);
            label.AutoSize = false;
            label.Size = new Size(width, TextHeight(text, font, width));
            return label;
        }

        public static Button MakeButton(Theme theme, string text, params string[] otherLabels)
        {
            var button = new Button
            {
                Text = text,
                Font = Body,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                BackColor = theme.Surface,
                ForeColor = theme.Text,
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderColor = theme.Border;
            button.FlatAppearance.MouseOverBackColor = theme.SurfaceHover;
            button.FlatAppearance.MouseDownBackColor = theme.Border;

            // Wide enough for every label the button will show.
            int textWidth = TextRenderer.MeasureText(text, Body).Width;
            foreach (string other in otherLabels) textWidth = Math.Max(textWidth, TextRenderer.MeasureText(other, Body).Width);
            button.Size = new Size(textWidth + S(32), S(32));
            return button;
        }

        public static int TextHeight(string text, Font font, int width)
        {
            return TextRenderer.MeasureText(text, font, new Size(width, int.MaxValue), TextFormatFlags.WordBreak).Height;
        }

        public static GraphicsPath RoundedRect(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }
            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        // amountOfA = 1 gives a, 0 gives b.
        public static Color Mix(Color a, Color b, float amountOfA)
        {
            float t = Math.Max(0f, Math.Min(1f, amountOfA));
            return Color.FromArgb(
                (int)Math.Round(a.R * t + b.R * (1 - t)),
                (int)Math.Round(a.G * t + b.G * (1 - t)),
                (int)Math.Round(a.B * t + b.B * (1 - t)));
        }

        // WCAG contrast ratio, from 1 to 21.
        public static double ContrastRatio(Color a, Color b)
        {
            double la = Luminance(a), lb = Luminance(b);
            return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
        }

        static double Luminance(Color color)
        {
            return 0.2126 * Linear(color.R) + 0.7152 * Linear(color.G) + 0.0722 * Linear(color.B);
        }

        static double Linear(int channel)
        {
            double c = channel / 255.0;
            return c <= 0.03928 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        static float ReadScale()
        {
            using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                return graphics.DpiX / 96f;
            }
        }
    }
}
