using System.Text;
using Microsoft.AspNetCore.Http.Extensions;
namespace ServiserOnline.Infrastructure;

public static class FileLogger
{
    private static readonly object Sync = new object();

    private static string GetLogPath()
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data", "serviser-log.txt");
        try
        {
            string dir = Path.GetDirectoryName(logPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
        catch { }
        return logPath;
    }

    public static void Log(string message)
    {
        try
        {
            string path = GetLogPath();
            string line = $"{DateTime.UtcNow:O} INFO  {message}{Environment.NewLine}";
            lock (Sync) { File.AppendAllText(path, line, Encoding.UTF8); }
        }
        catch { }
    }

    public static void LogWarning(string message)
    {
        try
        {
            string path = GetLogPath();
            string line = $"{DateTime.UtcNow:O} WARN  {message}{Environment.NewLine}";
            lock (Sync) { File.AppendAllText(path, line, Encoding.UTF8); }
        }
        catch { }
    }

    public static void LogException(Exception ex, HttpRequest request = null, string context = null)
    {
        try
        {
            string path = GetLogPath();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"{DateTime.UtcNow:O} ERROR  {context ?? "Exception occurred"}");
            if (request != null)
            {
                try
                {
                    sb.AppendLine("Request Url: " + request.GetDisplayUrl());
                    sb.AppendLine("Remote IP: " + request.HttpContext.Connection.RemoteIpAddress);
                    sb.AppendLine("X-Forwarded-For: " + (request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "n/a"));
                }
                catch { sb.AppendLine("  (Error while reading request headers)"); }
            }
            int idx = 1;
            Exception e = ex;
            while (e != null)
            {
                sb.AppendLine($"--- Exception Level {idx} ---");
                sb.AppendLine("Type: " + e.GetType().FullName);
                sb.AppendLine("Message: " + e.Message);
                sb.AppendLine("StackTrace:");
                sb.AppendLine(e.StackTrace ?? "(no stacktrace)");
                e = e.InnerException;
                idx++;
            }
            sb.AppendLine("----------------------------------------------------------");
            lock (Sync) { File.AppendAllText(path, sb.ToString(), Encoding.UTF8); }
        }
        catch { }
    }
}
