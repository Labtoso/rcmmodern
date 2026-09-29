using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RcmGui
{
    /// <summary>Laedt immer das neueste Hekate-Release von GitHub (CTCaer/hekate).</summary>
    internal static class Hekate
    {
        private const string ApiUrl = "https://api.github.com/repos/CTCaer/hekate/releases/latest";
        // Normales Payload, nicht die "_ram8GB"-Variante
        private static readonly Regex AssetRx = new Regex("\"browser_download_url\"\\s*:\\s*\"([^\"]*/(hekate_ctcaer_[0-9.]+\\.bin))\"");
        private static readonly Regex FileRx = new Regex(@"^hekate_ctcaer_[0-9.]+\.bin$", RegexOptions.IgnoreCase);

        public static bool IsHekate(string path) => FileRx.IsMatch(Path.GetFileName(path));

        public static string Version(string path)
        {
            var name = Path.GetFileNameWithoutExtension(path);
            return name.StartsWith("hekate_ctcaer_", StringComparison.OrdinalIgnoreCase) ? name.Substring(14) : "";
        }

        /// <summary>Lokal vorhandene Hekate-Version (falls offline).</summary>
        public static string FindLocal()
        {
            string best = null;
            foreach (var f in Directory.GetFiles(Paths.PayloadDir, "hekate_ctcaer_*.bin"))
                if (IsHekate(f) && (best == null || CompareVersion(Version(f), Version(best)) > 0))
                    best = f;
            return best;
        }

        public static async Task<string> GetLatestAsync(Action<string> log)
        {
            log("Suche neuestes Hekate auf GitHub ...");
            var json = await Net.Client.GetStringAsync(ApiUrl);
            var m = AssetRx.Match(json);
            if (!m.Success) throw new InvalidDataException("Kein Hekate-Payload im neuesten Release gefunden.");

            var target = Path.Combine(Paths.PayloadDir, m.Groups[2].Value);
            if (!File.Exists(target))
            {
                log("Lade " + m.Groups[2].Value + " ...");
                var bytes = await Net.Client.GetByteArrayAsync(m.Groups[1].Value);
                File.WriteAllBytes(target + ".tmp", bytes);
                if (File.Exists(target)) File.Delete(target);
                File.Move(target + ".tmp", target);
            }

            log("Hekate " + Version(target) + " ist aktuell.");
            return target;
        }

        private static int CompareVersion(string a, string b)
        {
            System.Version.TryParse(a, out var va);
            System.Version.TryParse(b, out var vb);
            return (va ?? new System.Version()).CompareTo(vb ?? new System.Version());
        }
    }
}
