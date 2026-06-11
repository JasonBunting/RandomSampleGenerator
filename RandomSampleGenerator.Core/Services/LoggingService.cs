namespace RandomSampleGenerator.Core.Services;

public sealed class LoggingService
{
    private readonly string _logFilePath;
    private readonly bool _enabled;
    private readonly Lock _lock = new();

    public LoggingService(string logFilePath, bool enabled)
    {
        _logFilePath = logFilePath;
        _enabled = enabled;

        if (_enabled)
        {
            var directory = Path.GetDirectoryName(logFilePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(logFilePath))
            {
                File.WriteAllText(logFilePath, string.Empty);
            }
        }
    }

    public void Info(string message)
    {
        if (!_enabled)
        {
            return;
        }

        lock (_lock)
        {
            File.AppendAllText(_logFilePath, $"{DateTimeOffset.UtcNow:O} [INFO] {message}{Environment.NewLine}");
        }
    }
}
