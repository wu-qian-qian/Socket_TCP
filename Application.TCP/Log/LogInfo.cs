namespace Log
{
    public static class LogInfo
    {
        static object _lock = new object();

        static string _pathName = System.Environment.CurrentDirectory;
        
        static LogInfo()
        {
            var str = Path.Combine(_pathName);
            var path = Path.Combine(str, "Log");
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        public static void WriteLog(string content)
        {
            File.AppendAllText(_pathName + $"/Log/{DateTime.Now.ToString("yyyy-MM-dd")}", $"{DateTime.Now}：" + content);
        }
        public static async Task WriteLogAsync(string content)
        {
           await Task.CompletedTask;
           lock(_lock)
            {
                File.AppendAllTextAsync(_pathName+ $"/Log/{DateTime.Now.ToString("yyyy-MM-dd")}", $"{DateTime.Now}：" + content);
            }
        }
    }
}