using System.Drawing;
using Microsoft.Win32;

namespace HideBar.UI
{
    // Colors that follow the Windows app mode (light or dark) and the accent color.
    class Theme
    {
        // WCAG minimums for text and for shapes such as a switch's track.
        const double MinimumTextContrast = 4.5;
        const double MinimumShapeContrast = 3.0;

        // Windows' default blue, from the lightest shade to the darkest.
        static readonly Color[] DefaultPalette =
        {
            Color.FromArgb(153, 235, 255), Color.FromArgb(76, 194, 255), Color.FromArgb(0, 145, 248),
            Color.FromArgb(0, 120, 212), Color.FromArgb(0, 103, 192), Color.FromArgb(0, 62, 146),
            Color.FromArgb(0, 26, 104)
        };

        public bool IsDark { get; private set; }
        public Color Background { get; private set; }
        public Color Surface { get; private set; }     // cards
        public Color SurfaceHover { get; private set; }
        public Color Border { get; private set; }
        public Color Text { get; private set; }
        public Color SubtleText { get; private set; }
        public Color Outline { get; private set; }     // a switch that is off
        public Color Accent { get; private set; }
        public Color AccentText { get; private set; }  // links, readable on the background
        public Color OnAccent { get; private set; }    // text and knobs drawn on the accent

        public static Theme FromWindows()
        {
            bool dark = ReadDword(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) == 0;
            Color[] palette = ReadAccentPalette();
            return Finish(dark ? Dark(palette) : Light(palette));
        }

        // Like Windows, dark mode uses the light shades of the accent and light mode the dark ones.
        static Theme Dark(Color[] palette)
        {
            return new Theme
            {
                IsDark = true,
                Background = Color.FromArgb(32, 32, 32),
                Surface = Color.FromArgb(43, 43, 43),
                SurfaceHover = Color.FromArgb(50, 50, 50),
                Border = Color.FromArgb(61, 61, 61),
                Text = Color.White,
                SubtleText = Color.FromArgb(171, 171, 171),
                Outline = Color.FromArgb(160, 160, 160),
                Accent = palette[1],
                AccentText = palette[0]
            };
        }

        static Theme Light(Color[] palette)
        {
            return new Theme
            {
                IsDark = false,
                Background = Color.FromArgb(243, 243, 243),
                Surface = Color.White,
                SurfaceHover = Color.FromArgb(249, 249, 249),
                Border = Color.FromArgb(225, 225, 225),
                Text = Color.FromArgb(27, 27, 27),
                SubtleText = Color.FromArgb(97, 97, 97),
                Outline = Color.FromArgb(118, 118, 118),
                Accent = palette[4],
                AccentText = palette[5]
            };
        }

        // Some accents, like a deep red or a dark gray, are too close to the background: a switch
        // that is on would look like one that is off, and links would be hard to read. Those are
        // pushed toward white in dark mode and toward black in light mode until they stand out.
        static Theme Finish(Theme theme)
        {
            Color toward = theme.IsDark ? Color.White : Color.Black;
            theme.Accent = WithContrast(theme.Accent, theme.Surface, MinimumShapeContrast, toward);
            theme.AccentText = WithContrast(theme.AccentText, theme.Background, MinimumTextContrast, toward);

            bool whiteReadsBetter = Style.ContrastRatio(theme.Accent, Color.White) >= Style.ContrastRatio(theme.Accent, Color.Black);
            theme.OnAccent = whiteReadsBetter ? Color.White : Color.Black;
            return theme;
        }

        static Color WithContrast(Color color, Color background, double minimum, Color toward)
        {
            Color result = color;
            for (float amount = 0.1f; amount < 1f && Style.ContrastRatio(result, background) < minimum; amount += 0.1f)
            {
                result = Style.Mix(toward, color, amount);
            }
            return result;
        }

        // Explorer keeps the accent's shades in AccentPalette: eight RGBA colors from the lightest
        // to the darkest, with the accent itself at index 3.
        static Color[] ReadAccentPalette()
        {
            var bytes = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\Accent",
                "AccentPalette", null) as byte[];
            if (bytes == null || bytes.Length < 28) return DefaultPalette;

            var palette = new Color[7];
            for (int i = 0; i < palette.Length; i++) palette[i] = Color.FromArgb(bytes[i * 4], bytes[i * 4 + 1], bytes[i * 4 + 2]);
            return palette;
        }

        static int ReadDword(string keyPath, string name, int fallback)
        {
            object value = Registry.GetValue(@"HKEY_CURRENT_USER\" + keyPath, name, null);
            return value is int ? (int)value : fallback;
        }
    }
}
