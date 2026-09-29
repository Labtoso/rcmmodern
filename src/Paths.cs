using System;
using System.IO;

namespace RcmGui
{
    /// <summary>Portabel: alles neben der EXE. Falls dort nicht schreibbar: %LOCALAPPDATA%.</summary>
    internal static class Paths
    {
        public static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string Root = IsWritable(AppDir)
            ? AppDir
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RcmGui");

        public static string BackendDir => Dir(Path.Combine(Root, "backend"));
        public static string DataDir => Dir(Path.Combine(Root, "data"));
        public static string PayloadDir => Dir(Path.Combine(DataDir, "payloads"));
        public static string SmashExe
        {
            get
            {
                var installed = Path.Combine(AppDir, "backend", "TegraRcmSmash.exe");
                return File.Exists(installed) ? installed : Path.Combine(BackendDir, "TegraRcmSmash.exe");
            }
        }
        public static string FavoritesFile => Path.Combine(DataDir, "favorites.txt");
        public static string WdiSimple => Path.Combine(AppDir, "wdi-simple.exe"); // Treiber-Installer neben der App

        private static string Dir(string path)
        {
            Directory.CreateDirectory(path);
            return path;
        }

        private static bool IsWritable(string dir)
        {
            try
            {
                var probe = Path.Combine(dir, ".write-test");
                File.WriteAllText(probe, "");
                File.Delete(probe);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
