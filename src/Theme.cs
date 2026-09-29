using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace RcmGui
{
    internal enum ButtonKind { Primary, Secondary }

    /// <summary>Ein Theme: dunkle Basis mit eigener Akzentfarbe.</summary>
    internal sealed class ThemeDef
    {
        public string Key;
        public string De, En;
        public Color Accent, AccentHover;
        public ThemeDef(string key, string de, string en, int r, int g, int b)
        {
            Key = key; De = de; En = en;
            Accent = Color.FromArgb(r, g, b);
            AccentHover = Color.FromArgb(Math.Min(255, r + 22), Math.Min(255, g + 22), Math.Min(255, b + 22));
        }
    }

    /// <summary>Immer 3 Themes: gleiche dunkle Basis, verschiedene Akzentfarben. Flach, keine Verlaeufe.</summary>
    internal static class Theme
    {
        // Immer genau 3 Themes (Farb-Akzente)
        public static readonly ThemeDef[] Themes =
        {
            new ThemeDef("blue",   "Blau",   "Blue",   47, 129, 247),
            new ThemeDef("green",  "Grün",   "Green",  46, 160, 67),
            new ThemeDef("purple", "Lila",   "Purple", 163, 113, 247),
        };

        public static string Current { get; private set; } = "blue";
        public static event Action Changed;

        public static Color Bg, Surface, SurfaceHover, Border, Text, Muted, Accent, AccentHover, Ok, Warn, Error;

        static Theme() { Apply("blue"); }

        public static ThemeDef Def(string key)
        {
            foreach (var t in Themes) if (t.Key == key) return t;
            return Themes[0];
        }

        public static void Apply(string key)
        {
            var def = Def(key);
            Current = def.Key;
            // Dunkle Basis (Sebastian mag dunkel)
            Bg = Color.FromArgb(13, 17, 23);
            Surface = Color.FromArgb(22, 27, 34);
            SurfaceHover = Color.FromArgb(30, 37, 46);
            Border = Color.FromArgb(48, 54, 61);
            Text = Color.FromArgb(230, 237, 243);
            Muted = Color.FromArgb(139, 148, 158);
            Accent = def.Accent;
            AccentHover = def.AccentHover;
            Ok = Color.FromArgb(46, 160, 67);
            Warn = Color.FromArgb(210, 153, 34);
            Error = Color.FromArgb(218, 54, 51);
            Changed?.Invoke();
        }

        public static readonly Font Body = new Font("Segoe UI", 9.75f);
        public static readonly Font Small = new Font("Segoe UI", 8.75f);
        public static readonly Font Label = new Font("Segoe UI Semibold", 8.75f);
        public static readonly Font Title = new Font("Segoe UI Semibold", 13f);
        public static readonly Font Mono = new Font("Consolas", 9f);

        public const float CardRadius = 12f;
        public const float ButtonRadius = 8f;

        public static RoundButton PrimaryButton(string text) => new RoundButton(ButtonKind.Primary) { Text = text };
        public static RoundButton SecondaryButton(string text) => new RoundButton(ButtonKind.Secondary) { Text = text };

        public static GraphicsPath RoundRect(RectangleF r, float radius)
        {
            var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static float Scale(Graphics g, float value) => value * g.DpiX / 96f;

        public static Icon AppIcon()
        {
            using (var s = typeof(Theme).Assembly.GetManifestResourceStream("app.ico"))
                return s == null ? null : new Icon(s);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        /// <summary>Titelleiste an das Theme anpassen (Windows 10 20H1+ / 11).</summary>
        public static void ApplyTitleBar(IntPtr handle)
        {
            var dark = 1; // Basis ist immer dunkel
            if (DwmSetWindowAttribute(handle, 20, ref dark, 4) != 0)
                DwmSetWindowAttribute(handle, 19, ref dark, 4); // aeltere Windows-10-Builds
            var caption = ColorTranslator.ToWin32(Bg);
            DwmSetWindowAttribute(handle, 35, ref caption, 4); // Titelleistenfarbe, nur Windows 11
            var round = 2;
            DwmSetWindowAttribute(handle, 33, ref round, 4);   // runde Fensterecken, nur Windows 11
        }
    }
}
