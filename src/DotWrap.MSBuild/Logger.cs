using System;
using System.IO;
using System.Text;

namespace DotWrap.MSBuild;

public class Logger
{
    private readonly StringBuilder _logBuilder = new StringBuilder();

    public void LogInfo(string message)
    {
        string log = $"[INFO] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public void LogWarning(string message)
    {
        string log = $"[WARN] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public void LogError(string message)
    {
        string log = $"[ERROR] {DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";
        _logBuilder.AppendLine(log);
    }

    public void SaveToFile(string filePath)
    {
        File.WriteAllText(filePath, _logBuilder.ToString());
    }

    public override string ToString()
    {
        return _logBuilder.ToString();
    }
}
