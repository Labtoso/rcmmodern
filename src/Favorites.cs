using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RcmGui
{
    /// <summary>Gemerkte Payloads, eine Datei pro Zeile in data/favorites.txt.</summary>
    internal static class Favorites
    {
        public static List<string> Load()
        {
            if (!File.Exists(Paths.FavoritesFile)) return new List<string>();
            return File.ReadAllLines(Paths.FavoritesFile)
                .Select(l => l.Trim())
                .Where(l => l.Length > 0 && File.Exists(l))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void Save(IEnumerable<string> paths)
        {
            // Hekate wird automatisch verwaltet und nicht gespeichert
            File.WriteAllLines(Paths.FavoritesFile, paths.Where(p => !Hekate.IsHekate(p)));
        }
    }
}
