using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RcmGui
{
    /// <summary>TegraRcmSmash von rajkosto: wird automatisch von der offiziellen Seite geladen.</summary>
    internal static class Backend
    {
        public const string DownloadUrl = "https://files.sshnuke.net/TegraRcmSmash1213.zip";

        public static bool IsReady => File.Exists(Paths.SmashExe);

        public static async Task EnsureAsync(Action<string> log)
        {
            if (IsReady) return;

            log("Lade TegraRcmSmash von sshnuke.net ...");
            var zipBytes = await Net.Client.GetByteArrayAsync(DownloadUrl);
            var arch = Environment.Is64BitOperatingSystem ? "x64/" : "Win32/";

            using (var zip = new ZipArchive(new MemoryStream(zipBytes)))
            {
                var exe = zip.Entries.FirstOrDefault(e => e.FullName.Equals(arch + "TegraRcmSmash.exe", StringComparison.OrdinalIgnoreCase))
                          ?? throw new InvalidDataException("TegraRcmSmash.exe nicht im Archiv gefunden.");
                var tmp = Paths.SmashExe + ".tmp";
                exe.ExtractToFile(tmp, true);
                if (File.Exists(Paths.SmashExe)) File.Delete(Paths.SmashExe);
                File.Move(tmp, Paths.SmashExe);

                foreach (var name in new[] { "LICENSE", "README.txt" })
                    zip.GetEntry(name)?.ExtractToFile(Path.Combine(Paths.BackendDir, name), true);
            }
            log("TegraRcmSmash bereit.");
        }

        /// <summary>Sendet den Payload an die Switch.</summary>
        public static Task<(bool ok, string output)> InjectAsync(string payload)
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo(Paths.SmashExe, "\"" + payload + "\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.Unicode, // TegraRcmSmash schreibt UTF-16
                    StandardErrorEncoding = Encoding.Unicode,
                    WorkingDirectory = Path.GetDirectoryName(Paths.SmashExe),
                };
                using (var p = Process.Start(psi))
                {
                    var stderr = p.StandardError.ReadToEndAsync();
                    var stdout = p.StandardOutput.ReadToEndAsync();
                    if (!p.WaitForExit(30000))
                    {
                        try { p.Kill(); } catch { }
                        return (false, "Abgebrochen: keine Antwort nach 30 s.");
                    }
                    var output = (stdout.Result + "\n" + stderr.Result).Trim();
                    return (p.ExitCode == 0, output);
                }
            });
        }
    }
}
