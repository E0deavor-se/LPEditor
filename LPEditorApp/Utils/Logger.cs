namespace LPEditorApp.Utils;

public enum LogLevel
{
    Info,
    Warn,
    Error,
    Debug
}

public interface ILogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message);
    void Debug(string message);
}

public class ConsoleLogger : ILogger
{
    public void Info(string message) => Log(LogLevel.Info, message);
    public void Warn(string message) => Log(LogLevel.Warn, message);
    public void Error(string message) => Log(LogLevel.Error, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);

    private static void Log(LogLevel level, string message)
    {
        var stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var line = $"[{stamp}] [{level}] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.WriteLine(line);
    }
}
