# Draws the HideBar icon (a monitor whose taskbar is a dashed line) and writes
# assets\HideBar.ico with every size Windows asks for, from 16 to 256 px.
#
#   powershell -ExecutionPolicy Bypass -File tools\make-icon.ps1 [-Preview preview.png]
#
# -Preview also saves a sheet with every size on a light and a dark background.

param(
    [string]$Output = (Join-Path $PSScriptRoot '..\assets\HideBar.ico'),
    [string]$Preview
)

Add-Type -ReferencedAssemblies System.Drawing -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

public static class HideBarIcon
{
    public static readonly int[] Sizes = { 16, 20, 24, 32, 40, 48, 64, 256 };

    // The drawing is laid out on a 16 x 16 grid. Small sizes snap to whole pixels so the
    // edges stay sharp.
    public static Bitmap Draw(int size)
    {
        float unit = size / 16f;
        bool snap = size <= 32;
        Func<float, float> p = v => snap ? (float)Math.Round(v * unit) : v * unit;

        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bitmap))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.Clear(Color.Transparent);

            using (var stand = new SolidBrush(Color.FromArgb(148, 163, 184)))
            {
                g.FillRectangle(stand, Box(p, 7, 12, 9, 14));
                FillRounded(g, stand, Box(p, 4, 14, 12, 15.75f), 0.75f * unit);
            }

            RectangleF screen = Box(p, 0.5f, 1.5f, 15.5f, 12);
            using (var fill = new LinearGradientBrush(screen, Color.FromArgb(96, 165, 250), Color.FromArgb(37, 99, 235), 90f))
            {
                FillRounded(g, fill, screen, Math.Max(1.5f, 2f * unit));
            }

            // The hidden taskbar.
            using (var dash = new SolidBrush(Color.FromArgb(240, 255, 255, 255)))
            {
                foreach (float x in new[] { 3f, 7f, 11f })
                {
                    RectangleF segment = Box(p, x, 10, x + 2, 11.25f);
                    if (segment.Height < 1) segment.Height = 1;
                    FillRounded(g, dash, segment, segment.Height / 2);
                }
            }
        }
        return bitmap;
    }

    public static void WriteIco(string path)
    {
        var images = new List<byte[]>();
        foreach (int size in Sizes)
        {
            using (Bitmap bitmap = Draw(size)) images.Add(size >= 256 ? Png(bitmap) : Dib(bitmap));
        }

        using (var stream = new FileStream(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)Sizes.Length);

            int offset = 6 + 16 * Sizes.Length;
            for (int i = 0; i < Sizes.Length; i++)
            {
                writer.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i]));
                writer.Write((byte)(Sizes[i] >= 256 ? 0 : Sizes[i]));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((short)1);
                writer.Write((short)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] image in images) writer.Write(image);
        }
    }

    public static void WritePreview(string path)
    {
        int cell = 280;
        using (var sheet = new Bitmap(cell * Sizes.Length, cell * 2))
        using (Graphics g = Graphics.FromImage(sheet))
        {
            g.InterpolationMode = InterpolationMode.NearestNeighbor;
            g.PixelOffsetMode = PixelOffsetMode.Half;
            Color[] backgrounds = { Color.FromArgb(243, 243, 243), Color.FromArgb(32, 32, 32) };
            for (int row = 0; row < 2; row++)
            {
                using (var brush = new SolidBrush(backgrounds[row])) g.FillRectangle(brush, 0, row * cell, sheet.Width, cell);
                for (int i = 0; i < Sizes.Length; i++)
                {
                    using (Bitmap icon = Draw(Sizes[i]))
                    {
                        // Small sizes are blown up so their pixels can be checked.
                        int shown = Sizes[i] >= 256 ? 256 : Sizes[i] * (240 / Sizes[i]);
                        g.DrawImage(icon, i * cell + (cell - shown) / 2, row * cell + (cell - shown) / 2, shown, shown);
                    }
                }
            }
            sheet.Save(path, ImageFormat.Png);
        }
    }

    static RectangleF Box(Func<float, float> p, float left, float top, float right, float bottom)
    {
        return RectangleF.FromLTRB(p(left), p(top), p(right), p(bottom));
    }

    static void FillRounded(Graphics g, Brush brush, RectangleF r, float radius)
    {
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        if (d <= 0) { g.FillRectangle(brush, r); return; }
        using (var path = new GraphicsPath())
        {
            path.AddArc(r.Left, r.Top, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Top, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.Left, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.FillPath(brush, path);
        }
    }

    static byte[] Png(Bitmap bitmap)
    {
        using (var stream = new MemoryStream())
        {
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }

    // A 32-bit DIB with straight alpha, bottom-up, followed by the 1-bit AND mask.
    static byte[] Dib(Bitmap bitmap)
    {
        int size = bitmap.Width;
        int maskStride = ((size + 31) / 32) * 4;
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(40);
            writer.Write(size);
            writer.Write(size * 2);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write(0);
            writer.Write(size * size * 4 + maskStride * size);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);

            for (int y = size - 1; y >= 0; y--)
            {
                for (int x = 0; x < size; x++)
                {
                    Color c = bitmap.GetPixel(x, y);
                    writer.Write(c.B);
                    writer.Write(c.G);
                    writer.Write(c.R);
                    writer.Write(c.A);
                }
            }
            for (int y = size - 1; y >= 0; y--)
            {
                var row = new byte[maskStride];
                for (int x = 0; x < size; x++)
                {
                    if (bitmap.GetPixel(x, y).A == 0) row[x / 8] |= (byte)(0x80 >> (x % 8));
                }
                writer.Write(row);
            }
            return stream.ToArray();
        }
    }
}
"@

$Output = [System.IO.Path]::GetFullPath($Output)
New-Item -ItemType Directory -Force -Path (Split-Path $Output) | Out-Null
[HideBarIcon]::WriteIco($Output)
"Wrote $Output"
if ($Preview) {
    $Preview = [System.IO.Path]::GetFullPath($Preview)
    [HideBarIcon]::WritePreview($Preview)
    "Wrote $Preview"
}
