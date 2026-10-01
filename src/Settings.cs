using System;
using System.Collections.Generic;
using System.IO;

namespace RcmGui
{
    /// <summary>App-Einstellungen, gespeichert als einfache INI in data\settings.ini.</summary>
    internal static class Settings
    {
        public static bool AutoInject;
        public static string Lang = "de";          // "de" oder "en"
        public static string Theme = "blue";       // Theme-Key (siehe Theme.Themes)

        private static string FilePath => Path.Combine(Paths.DataDir, "settings.ini");

        public static void Load()
        {
            try
            {
                if (!System.IO.File.Exists(FilePath)) return;
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in System.IO.File.ReadAllLines(FilePath))
                {
                    var i = line.IndexOf('=');
                    if (i > 0) map[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
                if (map.TryGetValue("AutoInject", out var ai)) AutoInject = ai == "1";
                if (map.TryGetValue("Lang", out var l) && (l == "de" || l == "en")) Lang = l;
                if (map.TryGetValue("Theme", out var t)) Theme = RcmGui.Theme.Def(t).Key; // ungueltig -> erstes Theme
            }
            catch (IOException) { }
        }

        public static void Save()
        {
            try
            {
                var text = "AutoInject=" + (AutoInject ? "1" : "0") + Environment.NewLine
                         + "Lang=" + Lang + Environment.NewLine
                         + "Theme=" + Theme + Environment.NewLine;
                System.IO.File.WriteAllText(FilePath, text);
            }
            catch (IOException) { }
        }
    }
}
