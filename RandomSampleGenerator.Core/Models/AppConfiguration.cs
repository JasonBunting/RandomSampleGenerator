namespace RandomSampleGenerator.Core.Models;

public sealed class AppConfiguration
{
    public string SourceFolderPath { get; set; } = string.Empty;

    public string TargetFolderPath { get; set; } = string.Empty;

    public int ExportSampleRate { get; set; } = 44100;

    public int ExportBitDepth { get; set; } = 16;

    public string ExportFormat { get; set; } = "wav";

    public bool AutoOpenOutputFolder { get; set; }

    public bool LoggingEnabled { get; set; } = true;

    public int MaxDistinctStemTypesPerSongPerRun { get; set; } = 3;

    public Dictionary<string, string> ModelByStemType { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
