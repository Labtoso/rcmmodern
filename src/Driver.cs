using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace RcmGui
{
    /// <summary>Installiert den libusbK-Treiber fuer die Switch ueber das mitgelieferte wdi-simple.exe.</summary>
    internal static class Driver
    {
        public static bool ToolAvailable => File.Exists(Paths.WdiSimple);

        public enum Result { Ok, Cancelled, Failed, ToolMissing }

        /// <summary>
        /// Startet wdi-simple mit Admin-Rechten (UAC) und Fortschrittsbalken.
        /// Schnell, wenn die Switch angesteckt ist (VID 0955 / PID 7321).
        /// </summary>
        public static Task<Result> InstallAsync()
        {
            return Task.Run(() =>
            {
                if (!ToolAvailable) return Result.ToolMissing;

                var psi = new ProcessStartInfo(Paths.WdiSimple,
                    "--vid 0x0955 --pid 0x7321 --type 2 --name \"Nintendo Switch (RCM)\" -b -o 60000")
                {
                    UseShellExecute = true,
                    Verb = "runas",              // UAC-Abfrage fuer die Treiberinstallation
                    WorkingDirectory = Paths.AppDir,
                };
                try
                {
                    using (var p = Process.Start(psi))
                    {
                        if (!p.WaitForExit(120000))
                        {
                            try { p.Kill(); } catch { }
                            return Result.Failed;
                        }
                        return p.ExitCode == 0 ? Result.Ok : Result.Failed;
                    }
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    return Result.Cancelled; // UAC abgebrochen
                }
                catch
                {
                    return Result.Failed;
                }
            });
        }
    }
}
