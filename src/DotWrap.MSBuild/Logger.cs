using System;
using System.IO;
using System.Text;

namespace DotWrap.MSBuild;

public static class Logger
{
    private static readonly StringBuilder _logBuilder = new StringBuilder();

    public static void LogDebug(string message)
    {
        string log = $"[DEBUG] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public static void LogInfo(string message)
    {
        string log = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public static void LogWarning(string message)
    {
        string log = $"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public static void LogError(string message)
    {
        string log = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public static void SaveToFile(string filePath)
    {
        File.WriteAllText(filePath, _logBuilder.ToString());
    }

    public static string ToString()
    {
        return _logBuilder.ToString();
    }
}
