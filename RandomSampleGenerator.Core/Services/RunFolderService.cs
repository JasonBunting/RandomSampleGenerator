namespace RandomSampleGenerator.Core.Services;

public sealed class RunFolderService
{
    public const string ManifestFileName = "run-manifest.json";
    private const string RunNamePrefix = " Sample Run ";

    public (string runName, string runFolderPath, string logFilePath) CreateRunArtifacts(string targetRootPath, DateTimeOffset now)
    {
        Directory.CreateDirectory(targetRootPath);

        var runPrefix = now.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture);
        var existingOrdinals = Directory.EnumerateDirectories(targetRootPath, $"{runPrefix}{RunNamePrefix}*", SearchOption.TopDirectoryOnly)
            .Select(path => Path.GetFileName(path))
            .Select(name => TryGetOrdinal(name, runPrefix, out var ordinal) ? ordinal : -1)
            .Where(value => value >= 0)
            .DefaultIfEmpty(-1);

        var nextOrdinal = existingOrdinals.Max() + 1;
        var runName = $"{runPrefix} Sample Run {nextOrdinal:00}";

        var runFolderPath = Path.Combine(targetRootPath, runName);
        Directory.CreateDirectory(runFolderPath);

        var logsPath = Path.Combine(targetRootPath, "logs");
        Directory.CreateDirectory(logsPath);
        var logFilePath = Path.Combine(logsPath, $"{runName}.log");

        return (runName, runFolderPath, logFilePath);
    }

    private static bool TryGetOrdinal(string? runFolderName, string runPrefix, out int ordinal)
    {
        ordinal = -1;

        if (string.IsNullOrWhiteSpace(runFolderName))
        {
            return false;
        }

        var expectedPrefix = $"{runPrefix}{RunNamePrefix}";
        if (!runFolderName.StartsWith(expectedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        var ordinalPart = runFolderName[expectedPrefix.Length..];
        return int.TryParse(ordinalPart, out ordinal);
    }
}
