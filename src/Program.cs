using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RcmGui
{
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            // GitHub und sshnuke.net verlangen TLS 1.2
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            // Wird vom Installer aufgerufen: Backend + Hekate laden, ohne Fenster
            if (args.Contains("--fetch-backend"))
                return FetchAll().GetAwaiter().GetResult() ? 0 : 1;

            Settings.Load();
            Theme.Apply(Settings.Theme);

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }

        private static async Task<bool> FetchAll()
        {
            try
            {
                await Backend.EnsureAsync(_ => { });
            }
            catch
            {
                return false;
            }
            try { await Hekate.GetLatestAsync(_ => { }); } catch { /* wird beim Start erneut versucht */ }
            return true;
        }
    }
}
