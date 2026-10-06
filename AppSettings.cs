namespace CustomExplorerApp
{
    public static class AppSettings
    {
        public static bool EnableWebUI { get; set; } = true;
        public static int WebPort { get; set; } = 9999;
        public static string LogFormat { get; set; } = "Text"; // "Text" or "JSON"
        public static string DefaultFolder { get; set; } = "";
        public static string LogFilePath { get; set; } = "explorer_logs.txt";
    }
}
