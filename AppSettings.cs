using System;
using System.IO;

namespace CustomExplorerApp
{
    public static class AppSettings
    {
        public static bool EnableWebUI { get; set; } = true;
        public static int WebPort { get; set; } = 9999;
        public static string LogFormat { get; set; } = "Text"; // "Text" or "JSON"
        public static string DefaultFolder { get; set; } = "";

        // Option 1: Dedicated Windows %LocalAppData%\LiveLog-Explorer\logs\ directory
        public static string LogDirectory
        {
            get
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string dir = Path.Combine(localAppData, "LiveLog-Explorer", "logs");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return dir;
            }
        }

        public static string LogFilePath => Path.Combine(LogDirectory, "explorer_logs.txt");
    }
}
